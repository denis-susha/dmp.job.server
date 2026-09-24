using DMP.BL.Constants;
using DMP.BL.Models;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMP.BL.Services;

/// <summary>
/// Processes <c>JobProductCacheTask</c> rows: loads the referenced products and writes them to Redis as JSON
/// documents keyed by their root category so they can be searched by the web API.
/// </summary>
public class ProductCacheService(
    ILogger<ProductCacheService> logger,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IRedisService redisService) : IProductCacheService
{
    private const int BatchSize = 10;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var allCategoriesDal = await redisService.GetCacheValueAsync<List<MenuCategoryDAL>>(CacheKeys.DALAllMenuCategoriesKey);

        if (allCategoriesDal is not { Count: > 0 })
        {
            throw new InvalidOperationException($"The key '{CacheKeys.DALAllMenuCategoriesKey}' is empty in Redis");
        }

        await using var context = await dmpContextFactory.CreateDbContextAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var tasks = await context.JobProductCacheTasks
                .OrderBy(t => t.JobProductCacheTaskId)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (tasks.Count == 0)
            {
                break;
            }

            try
            {
                await ProcessAsync(tasks, allCategoriesDal, context, cancellationToken);
                context.RemoveRange(tasks);
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cache products for tasks {TaskIds}", tasks.Select(t => t.JobProductCacheTaskId));
                throw;
            }
        }
    }

    private async Task ProcessAsync(
        List<JobProductCacheTaskDAL> tasks,
        List<MenuCategoryDAL> allCategories,
        DmpDbContext context,
        CancellationToken cancellationToken)
    {
        var productIds = tasks.Select(t => t.ProductId).ToList();

        var productsDal = await context.Products
            .Where(p => productIds.Contains(p.ProductId))
            .AsNoTracking()
            .Include(p => p.ProductFeatures!)
            .ThenInclude(pf => pf.Feature)
            .ToListAsync(cancellationToken);

        foreach (var productDal in productsDal)
        {
            var product = ToProduct(productDal);
            var rootId = FindRootCategoryId(allCategories, productDal.MenuCategoryId);
            await redisService.SetProductAsync(product, rootId);
        }
    }

    private static string FindRootCategoryId(List<MenuCategoryDAL> categories, int categoryId)
    {
        var category = categories.Find(c => c.MenuCategoryId == categoryId)
            ?? throw new InvalidOperationException($"Menu category {categoryId} is not found in the Redis category cache");

        return category.ParentId is { } parentId
            ? FindRootCategoryId(categories, parentId)
            : category.MenuCategoryId.ToString();
    }

    private static Product ToProduct(ProductDAL productDal)
    {
        var product = new Product
        {
            ProductId = productDal.ProductId,
            MenuCategoryId = productDal.MenuCategoryId.ToString(),
            UserFeatures = new UserFeature
            {
                Name = productDal.UserFeature?.Name,
                Description = productDal.UserFeature?.Description,
                Features = productDal.UserFeature?.Features,
            },
            Slug = productDal.Slug,
            SellerId = productDal.SellerId,
            Quantity = productDal.Quantity,
            Unlimited = productDal.Unlimited,
            ImgLinks = productDal.ImgLinks,
            Price = productDal.Price,
            IsLines = productDal.IsLines,
            CreatedAt = productDal.CreatedAt.ToUnixTimeSeconds(),
        };

        ApplyFeatures(productDal, product);

        return product;
    }

    /// <summary>
    /// Flattens product features into <c>Name_Value</c> search tokens. Range features (duration/year) are
    /// stored as dedicated numeric fields instead so they can be range-queried.
    /// </summary>
    private static void ApplyFeatures(ProductDAL productDal, Product product)
    {
        if (productDal.ProductFeatures is null)
        {
            return;
        }

        var result = new List<ProductFeature>();

        foreach (var productFeature in productDal.ProductFeatures)
        {
            var feature = productFeature.Feature!;

            if (feature.MultipleChoice == true)
            {
                result.AddRange(productFeature.Value
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => new ProductFeature { Key = feature.Name, Value = $"{feature.Name}_{value}" }));
            }
            else if (feature.Type == FeatureType.Range)
            {
                switch (feature.RangeType)
                {
                    case FeatureRangeType.Duration:
                        product.Duration = int.Parse(productFeature.Value);
                        break;
                    case FeatureRangeType.Year:
                        product.Year = int.Parse(productFeature.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(productDal), feature.RangeType, $"Unsupported range type for feature {feature.FeatureId}");
                }
            }
            else
            {
                result.Add(new ProductFeature { Key = feature.Name, Value = $"{feature.Name}_{productFeature.Value}" });
            }
        }

        product.Features = result;
    }
}

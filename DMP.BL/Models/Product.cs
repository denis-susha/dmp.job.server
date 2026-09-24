namespace DMP.BL.Models;

/// <summary>
/// Product document stored as RedisJSON under <c>product:{rootCategoryId}:{productId}</c>
/// and indexed by the web API's RediSearch indexes. Property names form the JSON contract.
/// </summary>
public class Product
{
    public int ProductId { get; set; }
    public required string MenuCategoryId { get; set; }
    public UserFeature? UserFeatures { get; set; }
    public required string Slug { get; set; }
    public Guid SellerId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool Unlimited { get; set; }
    public string[]? ImgLinks { get; set; }
    public List<ProductFeature>? Features { get; set; }
    public bool IsLines { get; set; }
    public long CreatedAt { get; set; }

    public int? Duration { get; set; }
    public int? Year { get; set; }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Order;
using DMP.DataAccess.Models.Sale;
using Microsoft.EntityFrameworkCore;

namespace DMP.DataAccess;

public class DmpDbContext(DbContextOptions<DmpDbContext> options) : DbContext(options)
{
    private static readonly JsonSerializerOptions _jsonbWriteOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    private static readonly JsonSerializerOptions _jsonbReadOptions = new() { PropertyNameCaseInsensitive = true };

    public DbSet<MenuDAL> Menus => Set<MenuDAL>();
    public DbSet<MenuCategoryDAL> MenuCategories => Set<MenuCategoryDAL>();
    public DbSet<TranslationDAL> Translations => Set<TranslationDAL>();
    public DbSet<ProductDAL> Products => Set<ProductDAL>();
    public DbSet<ProductCreationDAL> ProductCreations => Set<ProductCreationDAL>();
    public DbSet<ProductImageDAL> ProductImages => Set<ProductImageDAL>();
    public DbSet<FeatureDAL> Features => Set<FeatureDAL>();
    public DbSet<ProductFeatureDAL> ProductFeatures => Set<ProductFeatureDAL>();
    public DbSet<MailDAL> Mails => Set<MailDAL>();
    public DbSet<UserDAL> Users => Set<UserDAL>();
    public DbSet<EmailConfirmationDAL> EmailConfirmations => Set<EmailConfirmationDAL>();
    public DbSet<ApplicationSettingsDAL> ApplicationSettings => Set<ApplicationSettingsDAL>();
    public DbSet<OrderHeaderDAL> OrderHeaders => Set<OrderHeaderDAL>();
    public DbSet<OrderLineDAL> OrderLines => Set<OrderLineDAL>();
    public DbSet<CartItemDAL> CartItems => Set<CartItemDAL>();
    public DbSet<InvoiceWorkerTaskDAL> InvoiceWorkerTasks => Set<InvoiceWorkerTaskDAL>();
    public DbSet<SalesHeaderDAL> SalesHeaders => Set<SalesHeaderDAL>();
    public DbSet<SalesLineDAL> SalesLines => Set<SalesLineDAL>();
    public DbSet<CategoryFeatureDAL> CategoryFeatures => Set<CategoryFeatureDAL>();
    public DbSet<ProductFileDAL> ProductFiles => Set<ProductFileDAL>();
    public DbSet<ProductLineDAL> ProductLines => Set<ProductLineDAL>();
    public DbSet<EmailTemplateDAL> EmailTemplates => Set<EmailTemplateDAL>();
    public DbSet<JobProductCacheTaskDAL> JobProductCacheTasks => Set<JobProductCacheTaskDAL>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuDAL>(entity =>
        {
            entity.ToTable("Menu");
            entity.HasKey(p => p.MenuId);

            entity.Property(p => p.Description)
                .HasColumnName("Description")
                .HasMaxLength(100);
        });

        modelBuilder.Entity<MenuCategoryDAL>(entity =>
        {
            entity.ToTable("MenuCategory");
            entity.HasKey(p => p.MenuCategoryId);

            entity.Property(e => e.MenuId)
                .HasColumnName("MenuId");

            entity.Property(p => p.Name)
                .IsRequired()
                .HasColumnName("Name")
                .HasMaxLength(100);

            entity.Property(p => p.ParentId)
                .HasColumnName("ParentId");

            entity.Property(p => p.Order)
                .HasColumnName("Order");

            entity.Property(p => p.Slug)
                .IsRequired()
                .HasColumnName("Slug")
                .HasMaxLength(5000);

            entity.HasOne(p => p.Menu)
                .WithMany(m => m.Categories)
                .HasForeignKey(k => k.MenuId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Menu_MenuId_MenuId");

            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MenuCategory_ParentId");
        });

        modelBuilder.Entity<TranslationDAL>(entity =>
        {
            entity.ToTable("Translation");
            entity.HasKey(p => p.Key);

            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(t => t.Translations)
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<ProductDAL>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(p => p.ProductId);
            entity.Property(e => e.ProductId).ValueGeneratedOnAdd();
            entity.Property(e => e.MenuCategoryId)
                .IsRequired();

            entity.Property(t => t.UserFeature)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonbWriteOptions),
                    v => JsonSerializer.Deserialize<UserFeatureDAL>(v, _jsonbReadOptions)
                )
                .HasColumnType("jsonb");

            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(5000);

            entity.Property(e => e.SellerId).IsRequired();
            entity.Property(e => e.Price).IsRequired();
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.Unlimited).IsRequired();
            entity.Property(e => e.ImgLinks);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();

            entity.HasOne(p => p.MenuCategory)
                .WithMany(m => m.Products)
                .HasForeignKey(k => k.MenuCategoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MenuCategory_MenuCategoryId");

            entity.HasOne(cf => cf.ProductCreation)
                .WithOne(f => f.Product)
                .HasForeignKey<ProductCreationDAL>(p => p.ProductId)
                .HasConstraintName("FK_ProductCreation_Product");
        });

        modelBuilder.Entity<ProductCreationDAL>(entity =>
        {
            entity.ToTable("ProductCreation");
            entity.HasKey(p => p.ProductCreationId);
            entity.Property(e => e.ProductCreationId).ValueGeneratedOnAdd();
            entity.Property(e => e.ProductId);
            entity.Property(e => e.CustomCategory);
            entity.Property(e => e.UseCustomCategory).IsRequired();
            entity.Property(e => e.Lines);
            entity.Property(e => e.MessageForSeller);
        });

        modelBuilder.Entity<ProductImageDAL>(entity =>
        {
            entity.ToTable("ProductImage");
            entity.HasKey(p => new { p.ProductId, p.Hash });

            entity.Property(e => e.Hash).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Images).IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonbWriteOptions),
                    v => JsonSerializer.Deserialize<StoredImageDAL>(v, _jsonbReadOptions)!
                )
                .HasColumnType("jsonb");
            entity.Property(e => e.IsCover).IsRequired();
            entity.Property(e => e.Attached).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(31);

            entity.HasOne(cf => cf.Product)
                .WithMany(f => f.ProductImages)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductImage_Product");
        });

        modelBuilder.Entity<FeatureDAL>(entity =>
        {
            entity.ToTable("Feature");
            entity.HasKey(p => p.FeatureId);

            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.CheckboxesValues);
            entity.Property(e => e.RangeMin);
            entity.Property(e => e.RangeMax);
            entity.Property(e => e.RangeFrom);
            entity.Property(e => e.RangeTo);
            entity.Property(e => e.MultipleChoice);
            entity.Property(e => e.RangeType);
        });

        modelBuilder.Entity<ProductFeatureDAL>(entity =>
        {
            entity.ToTable("ProductFeature");
            entity.HasKey(p => new { p.ProductId, p.FeatureId });

            entity.Property(e => e.Value);

            entity.HasOne(p => p.Product)
                .WithMany(i => i.ProductFeatures)
                .HasForeignKey(p => p.ProductId);

            entity.HasOne(p => p.Feature)
                .WithMany(f => f.ProductFeatures)
                .HasForeignKey(p => p.FeatureId);
        });

        modelBuilder.Entity<MailDAL>(entity =>
        {
            entity.ToTable("Mail");
            entity.HasKey(p => p.MailId);

            entity.Property(e => e.MailId).HasColumnName("MailId").IsRequired();
            entity.Property(e => e.To).HasColumnName("To").IsRequired().HasMaxLength(320);
            entity.Property(e => e.From).HasColumnName("From").IsRequired().HasMaxLength(320);
            entity.Property(e => e.Subject).HasColumnName("Subject").HasMaxLength(988);
            entity.Property(e => e.Copy).HasColumnName("Copy").HasMaxLength(1000);
            entity.Property(e => e.Body).HasColumnName("Body");
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql();
            entity.Property(e => e.Attempts).HasColumnName("Attempts").IsRequired();
            entity.Property(e => e.EmailTemplateId);
            entity.Property(t => t.Model);

            entity.HasOne(p => p.EmailTemplate)
                .WithMany(f => f.Mails)
                .HasForeignKey(p => p.EmailTemplateId);
        });

        modelBuilder.Entity<UserDAL>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(p => p.UserId);

            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.Email).HasColumnName("Email").IsRequired().HasMaxLength(320);
            entity.Property(e => e.Password).HasColumnName("Password").IsRequired();
            entity.Property(e => e.Salt).HasColumnName("Salt").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.Status).HasColumnName("Status").HasDefaultValueSql();
            entity.Property(e => e.Flags).HasColumnName("Flags").IsRequired();
        });

        modelBuilder.Entity<EmailConfirmationDAL>(entity =>
        {
            entity.ToTable("EmailConfirmation");
            entity.HasKey(p => p.EmailConfirmationId);

            entity.Property(e => e.EmailConfirmationId).HasColumnName("EmailConfirmationId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.Token).HasColumnName("Token").IsRequired();
            entity.Property(e => e.Used).HasColumnName("Used").HasDefaultValueSql();
        });

        modelBuilder.Entity<ApplicationSettingsDAL>(entity =>
        {
            entity.ToTable("ApplicationSettings");
            entity.HasKey(p => p.Key);

            entity.Property(e => e.Key).HasColumnName("Key").IsRequired();
            entity.Property(e => e.Value).HasColumnName("Value").HasColumnType("jsonb");
        });

        modelBuilder.Entity<OrderHeaderDAL>(entity =>
        {
            entity.ToTable("OrderHeader");
            entity.HasKey(p => p.OrderId);
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.ReferenceNumber).HasColumnName("ReferenceNumber").HasMaxLength(200);
            entity.Property(e => e.Currency).HasColumnName("Currency").IsRequired();

        });

        modelBuilder.Entity<OrderLineDAL>(entity =>
        {
            entity.ToTable("OrderLine");
            entity.HasKey(p => p.OrderLineId);
            entity.Property(p => p.OrderLineId).IsRequired();
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("ProductId").IsRequired();
            entity.Property(e => e.SellerId).HasColumnName("SellerId").IsRequired();
            entity.Property(e => e.Price).HasColumnName("Price").IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("Quantity").IsRequired();
            entity.Property(e => e.Currency).HasColumnName("Currency").IsRequired();
            entity.Property(e => e.IsLine).HasColumnName("IsLine").IsRequired();
            entity.Property(e => e.ProductFileId).HasColumnName("ProductFileId");
            entity.Property(e => e.ProductLineId).HasColumnName("ProductLineId");

            entity.HasOne(p => p.OrderHeader)
                .WithMany(m => m.OrderLines)
                .HasForeignKey(k => k.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_OrderLine_OrderId_OrderHeader_OrderId");
        });

        modelBuilder.Entity<CartItemDAL>(entity =>
        {
            entity.ToTable("CartItem");
            entity.HasKey(p => new { p.UserId, p.ProductId });
            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("ProductId").IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("Quantity").IsRequired();
            entity.Property(e => e.Selected).HasColumnName("Selected").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
        });

        modelBuilder.Entity<InvoiceWorkerTaskDAL>(entity =>
        {
            entity.ToTable("InvoiceWorkerTask");
            entity.HasKey(p => p.InvoiceId);
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
        });

        modelBuilder.Entity<SalesHeaderDAL>(entity =>
        {
            entity.ToTable("SalesHeader");
            entity.HasKey(p => p.SalesHeaderId);
            entity.Property(e => e.SellerId).HasColumnName("SellerId").IsRequired();
            entity.Property(e => e.Amount).HasColumnName("Amount").IsRequired();
            entity.Property(e => e.Currency).HasColumnName("Currency").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
        });

        modelBuilder.Entity<SalesLineDAL>(entity =>
        {
            entity.ToTable("SalesLine");
            entity.HasKey(p => p.SalesLineId);
            entity.Property(p => p.SalesHeaderId).HasColumnName("SalesHeaderId").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("ProductId").IsRequired();
            entity.Property(e => e.Price).HasColumnName("Price").IsRequired();
            entity.Property(e => e.Currency).HasColumnName("Currency").IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("Quantity").IsRequired();
            entity.Property(e => e.TransactionId).HasColumnName("TransactionId").IsRequired().HasMaxLength(26);

            entity.HasOne(p => p.SalesHeader)
                .WithMany(m => m.SalesLines)
                .HasForeignKey(k => k.SalesHeaderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SalesLine_SalesHeaderId_SalesHeader_SalesHeaderId");

            entity.HasOne(p => p.Product)
                .WithMany(m => m.SalesLines)
                .HasForeignKey(k => k.ProductId)
                .HasConstraintName("FK_SalesLine_ProductId_Product_ProductId");
        });

        modelBuilder.Entity<CategoryFeatureDAL>(entity =>
        {
            entity.ToTable("CategoryFeature");
            entity.HasKey(p => new { p.CategoryId, p.FeatureId });

            entity.HasOne(cf => cf.Feature)
                .WithMany(f => f.CategoryFeatures)
                .HasForeignKey(cf => cf.FeatureId);

            entity.HasOne(cf => cf.MenuCategory)
                .WithMany(f => f.CategoryFeatures)
                .HasForeignKey(cf => cf.CategoryId);
        });

        modelBuilder.Entity<ProductFileDAL>(entity =>
        {
            entity.ToTable("ProductFile");
            entity.HasKey(p => p.ProductFileId);
            entity.Property(p => p.ProductId).IsRequired();
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.FileSize).IsRequired();
            entity.Property(p => p.FileName).IsRequired();
            entity.Property(p => p.StoragePath).IsRequired();
            entity.Property(p => p.IsUploaded).IsRequired();

            entity.HasOne(cf => cf.Product)
                .WithMany(f => f.ProductFiles)
                .HasForeignKey(cf => cf.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductFile_Product");
        });

        modelBuilder.Entity<ProductLineDAL>(entity =>
        {
            entity.ToTable("ProductLine");
            entity.HasKey(p => p.ProductLineId);
            entity.Property(p => p.ProductId).IsRequired();
            entity.Property(p => p.Value).IsRequired();
            entity.Property(p => p.IsSold).IsRequired();

            entity.HasOne(cf => cf.Product)
                .WithMany(f => f.ProductLines)
                .HasForeignKey(cf => cf.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductLines_Product");
        });

        modelBuilder.Entity<EmailTemplateDAL>(entity =>
        {
            entity.ToTable("EmailTemplate");
            entity.HasKey(p => p.EmailTemplateId);
            entity.Property(p => p.Name).IsRequired();
            entity.Property(p => p.Subject).IsRequired();
            entity.Property(p => p.Body).IsRequired();
            entity.Property(p => p.Language).IsRequired();
        });

        modelBuilder.Entity<JobProductCacheTaskDAL>(entity =>
        {
            entity.ToTable("JobProductCacheTask");
            entity.HasKey(p => p.JobProductCacheTaskId);
            entity.Property(p => p.ProductId).IsRequired();
            entity.Property(p => p.CreatedAt).HasDefaultValueSql();
        });

        base.OnModelCreating(modelBuilder);
    }
}

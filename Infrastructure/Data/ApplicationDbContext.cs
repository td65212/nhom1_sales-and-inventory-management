using Microsoft.EntityFrameworkCore;
using nhom1_sales_and_inventory_management.Domain.Entities;

namespace nhom1_sales_and_inventory_management.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    public DbSet<ProductImage> ProductImages { get; set; }

    public DbSet<ProductVariant> ProductVariants { get; set; }

    public DbSet<ProductVariantColor> ProductVariantColors { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Inventory> Inventories { get; set; }

    public DbSet<StockReceipt> StockReceipts { get; set; }

    public DbSet<StockReceiptItem> StockReceiptItems { get; set; }

    public DbSet<StockEvent> StockEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);

        modelBuilder.Entity<Product>()
            .HasIndex(product => product.SupplierId);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Inventory)
            .WithOne(i => i.Product)
            .HasForeignKey<Inventory>(i => i.ProductId);

        modelBuilder.Entity<Product>()
            .HasMany(product => product.Variants)
            .WithOne(variant => variant.Product)
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductVariant>()
            .HasIndex(variant => variant.Sku)
            .IsUnique();

        modelBuilder.Entity<ProductVariant>()
            .Property(variant => variant.OriginalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ProductVariant>()
            .Property(variant => variant.SalePrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ProductVariant>()
            .HasMany(variant => variant.Colors)
            .WithOne(color => color.ProductVariant)
            .HasForeignKey(color => color.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductVariantColor>()
            .HasIndex(color => new { color.ProductVariantId, color.Name })
            .IsUnique();

        modelBuilder.Entity<ProductVariantColor>()
            .HasMany(color => color.Images)
            .WithOne(image => image.ProductVariantColor)
            .HasForeignKey(image => image.ProductVariantColorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductImage>()
            .HasIndex(image => new { image.ProductVariantColorId, image.SortOrder });

        modelBuilder.Entity<ProductVariant>()
            .Property(variant => variant.Name)
            .HasMaxLength(80);

        modelBuilder.Entity<ProductVariant>()
            .Property(variant => variant.Sku)
            .HasMaxLength(120);

        modelBuilder.Entity<ProductVariantColor>()
            .Property(color => color.Name)
            .HasMaxLength(80);

        modelBuilder.Entity<Product>()
            .Property(product => product.ImportPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(product => product.SellingPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(product => product.OriginalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(product => product.SalePrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<StockReceipt>()
            .Property(receipt => receipt.ImportDate)
            .HasColumnType("date");

        modelBuilder.Entity<StockReceipt>()
            .HasMany(receipt => receipt.Items)
            .WithOne(item => item.StockReceipt)
            .HasForeignKey(item => item.StockReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockReceiptItem>()
            .HasOne(item => item.Product)
            .WithMany(product => product.StockReceiptItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockReceiptItem>()
            .Property(item => item.ImportPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<StockEvent>()
            .HasIndex(stockEvent => stockEvent.EventId)
            .IsUnique();

        modelBuilder.Entity<StockEvent>()
            .HasIndex(stockEvent => stockEvent.OccurredAt);

        modelBuilder.Entity<StockEvent>()
            .HasIndex(stockEvent => new
            {
                stockEvent.ProductId,
                stockEvent.Source,
                stockEvent.ReferenceId
            })
            .IsUnique()
            .HasFilter("\"ReferenceId\" IS NOT NULL");
    }
}

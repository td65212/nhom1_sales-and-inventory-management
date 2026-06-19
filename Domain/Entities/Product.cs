namespace nhom1_sales_and_inventory_management.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal ImportPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal OriginalPrice { get; set; }

    public decimal? SalePrice { get; set; }

    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public int SupplierId { get; set; }

    public Inventory Inventory { get; set; } = null!;

    public ICollection<ProductVariant> Variants { get; set; }
        = new List<ProductVariant>();

    public ICollection<StockReceiptItem> StockReceiptItems { get; set; }
        = new List<StockReceiptItem>();
}

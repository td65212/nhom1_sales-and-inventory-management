namespace nhom1_sales_and_inventory_management.Domain.Entities;

public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public int Quantity { get; set; }
    public int ReserveStock { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ProductVariantColor> Colors { get; set; } = new List<ProductVariantColor>();
}

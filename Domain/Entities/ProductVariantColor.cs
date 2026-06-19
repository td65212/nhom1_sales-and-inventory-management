namespace nhom1_sales_and_inventory_management.Domain.Entities;

public class ProductVariantColor
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? HexCode { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}

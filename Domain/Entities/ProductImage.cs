namespace nhom1_sales_and_inventory_management.Domain.Entities;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductVariantColorId { get; set; }

    public ProductVariantColor ProductVariantColor { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

namespace nhom1_sales_and_inventory_management.DTOs.Product;

public class ProductImageItemDto
{
    public int Id { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

namespace nhom1_sales_and_inventory_management.Domain.Entities;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;

    public string? Version { get; set; }

    public int SortOrder { get; set; }
}

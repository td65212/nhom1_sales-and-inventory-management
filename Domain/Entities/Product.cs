namespace nhom1_sales_and_inventory_management.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal ImportPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public Inventory Inventory { get; set; } = null!;
}
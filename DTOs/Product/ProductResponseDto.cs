namespace nhom1_sales_and_inventory_management.DTOs.Product;

public class ProductResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal ImportPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int ReserveStock { get; set; }
}

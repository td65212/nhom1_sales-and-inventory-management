namespace nhom1_sales_and_inventory_management.DTOs.Product;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal ImportPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal? OriginalPrice { get; set; }

    public decimal? SalePrice { get; set; }

    public string? ImageUrl { get; set; }

    public List<string> ImageUrls { get; set; } = new();

    public List<ProductImageItemDto> ImageItems { get; set; } = new();

    public int CategoryId { get; set; }

    public int SupplierId { get; set; }

    public int Quantity { get; set; }

    public int ReserveStock { get; set; }
}

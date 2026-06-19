namespace nhom1_sales_and_inventory_management.DTOs.Product;

public class ProductVariantDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Quantity { get; set; }
    public int ReserveStock { get; set; }
    public bool IsActive { get; set; }
    public List<ProductVariantColorDto> Colors { get; set; } = new();
}

public class ProductVariantColorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? HexCode { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; }
    public List<ProductImageItemDto> Images { get; set; } = new();
}

public class UpsertProductVariantDto
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public int Quantity { get; set; }
    public int ReserveStock { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpsertProductVariantColorDto
{
    public string Name { get; set; } = string.Empty;
    public string? HexCode { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> ImageUrls { get; set; } = new();
}

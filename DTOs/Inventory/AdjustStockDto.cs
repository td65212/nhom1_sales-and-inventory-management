namespace nhom1_sales_and_inventory_management.DTOs.Inventory;

public class AdjustStockDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? ReferenceId { get; set; }
}

public class AdjustStockResponseDto
{
    public required ProductStockDto Product { get; set; }
    public int PreviousQuantity { get; set; }
    public int CurrentQuantity { get; set; }
}

public class ProductStockDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReserveStock { get; set; }
}

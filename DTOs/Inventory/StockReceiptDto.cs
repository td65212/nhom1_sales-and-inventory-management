namespace nhom1_sales_and_inventory_management.DTOs.Inventory;

public class CreateStockReceiptDto
{
    public int SupplierId { get; set; }
    public string? Note { get; set; }
    public List<CreateStockReceiptItemDto> Items { get; set; } = new();
}

public class CreateStockReceiptItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal ImportPrice { get; set; }
}

public class StockReceiptResponseDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public List<StockReceiptItemResponseDto> Items { get; set; } = new();
}

public class StockReceiptItemResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal ImportPrice { get; set; }
}

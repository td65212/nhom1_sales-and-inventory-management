namespace nhom1_sales_and_inventory_management.Domain.Entities;

public class StockReceiptItem
{
    public int Id { get; set; }
    public int StockReceiptId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal ImportPrice { get; set; }
    public StockReceipt StockReceipt { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

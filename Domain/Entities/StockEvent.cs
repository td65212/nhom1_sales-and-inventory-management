namespace nhom1_sales_and_inventory_management.Domain.Entities;

public class StockEvent
{
    public int Id { get; set; }
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = "stock.updated";
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int PreviousQuantity { get; set; }
    public int CurrentQuantity { get; set; }
    public int QuantityChange { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

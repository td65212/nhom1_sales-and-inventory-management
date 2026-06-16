namespace nhom1_sales_and_inventory_management.Domain.Entities;

public class StockReceipt
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
    public StockReceiptStatus Status { get; set; } = StockReceiptStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public ICollection<StockReceiptItem> Items { get; set; } = new List<StockReceiptItem>();
}

public enum StockReceiptStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3
}

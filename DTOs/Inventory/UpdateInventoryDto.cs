namespace nhom1_sales_and_inventory_management.DTOs.Inventory;

public class UpdateInventoryDto
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public int ReserveStock { get; set; }
}
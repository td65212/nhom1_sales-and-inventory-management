namespace nhom1_sales_and_inventory_management.DTOs.Integration;

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

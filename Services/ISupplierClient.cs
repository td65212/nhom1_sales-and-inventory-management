using nhom1_sales_and_inventory_management.DTOs.Integration;

namespace nhom1_sales_and_inventory_management.Services;

public interface ISupplierClient
{
    Task<SupplierDto?> GetByIdAsync(int id);
}

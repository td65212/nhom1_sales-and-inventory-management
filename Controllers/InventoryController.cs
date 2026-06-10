using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhom1_sales_and_inventory_management.DTOs.Inventory;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

namespace nhom1_sales_and_inventory_management.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InventoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetInventory()
    {
        var data = await _context.Inventories
            .Include(x => x.Product)
            .Select(x => new
            {
                x.ProductId,
                ProductName = x.Product.Name,
                x.Quantity,
                x.ReserveStock
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("reserve-stock")]
    public async Task<IActionResult> GetReserveStock()
    {
        var data = await _context.Inventories
            .Include(x => x.Product)
            .Select(x => new
            {
                x.ProductId,
                ProductName = x.Product.Name,
                x.ReserveStock
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var data = await _context.Inventories
            .Include(x => x.Product)
            .Where(x => x.Quantity <= x.ReserveStock)
            .Select(x => new
            {
                x.ProductId,
                ProductName = x.Product.Name,
                x.Quantity,
                x.ReserveStock
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateInventory(
        UpdateInventoryDto dto)
    {
        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(x => x.ProductId == dto.ProductId);

        if (inventory == null)
            return NotFound();

        inventory.Quantity = dto.Quantity;
        inventory.ReserveStock = dto.ReserveStock;

        await _context.SaveChangesAsync();

        return Ok(inventory);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhom1_sales_and_inventory_management.Domain.Entities;
using nhom1_sales_and_inventory_management.DTOs.Inventory;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

namespace nhom1_sales_and_inventory_management.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "Admin,WarehouseKeeper")]
public class InventoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public InventoryController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetInventory()
    {
        var data = await _context.Inventories
            .AsNoTracking()
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
            .AsNoTracking()
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
            .AsNoTracking()
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

    [HttpGet("events")]
    public async Task<IActionResult> GetStockEvents(
        [FromQuery] DateTime? after = null,
        [FromQuery] int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 500);
        var query = _context.StockEvents.AsNoTracking().AsQueryable();

        if (after.HasValue)
            query = query.Where(stockEvent => stockEvent.OccurredAt > after.Value);

        var events = await query
            .OrderBy(stockEvent => stockEvent.OccurredAt)
            .Take(limit)
            .ToListAsync();

        return Ok(events);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateInventory(UpdateInventoryDto dto)
    {
        if (dto.Quantity < 0 || dto.ReserveStock < 0)
            return BadRequest(new { message = "Tồn kho và ngưỡng cảnh báo không được âm" });

        var inventory = await _context.Inventories
            .Include(value => value.Product)
            .FirstOrDefaultAsync(value => value.ProductId == dto.ProductId);

        if (inventory is null)
            return NotFound();

        var previousQuantity = inventory.Quantity;
        inventory.Quantity = dto.Quantity;
        inventory.ReserveStock = dto.ReserveStock;
        AddStockEvent(inventory, previousQuantity, "manual.adjustment", null);
        await _context.SaveChangesAsync();

        return Ok(inventory);
    }

    [HttpPost("reserve")]
    [AllowAnonymous]
    public async Task<IActionResult> ReserveStock(AdjustStockDto dto)
    {
        if (!HasValidInternalApiKey())
            return Unauthorized(new { message = "Internal API key không hợp lệ" });

        if (dto.ProductId <= 0 || dto.Quantity <= 0)
            return BadRequest(new { message = "ProductId và số lượng phải lớn hơn 0" });

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var affected = await _context.Inventories
            .Where(inventory => inventory.ProductId == dto.ProductId
                && inventory.Quantity >= dto.Quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    inventory => inventory.Quantity,
                    inventory => inventory.Quantity - dto.Quantity));

        if (affected == 0)
        {
            var exists = await _context.Inventories
                .AnyAsync(inventory => inventory.ProductId == dto.ProductId);
            return exists
                ? Conflict(new { message = "Không đủ tồn kho" })
                : NotFound(new { message = "Không tìm thấy sản phẩm" });
        }

        var product = await LoadProductStockAsync(dto.ProductId);
        var previousQuantity = product.Quantity + dto.Quantity;
        _context.StockEvents.Add(CreateStockEvent(
            product,
            previousQuantity,
            "order.reserved",
            dto.ReferenceId));
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new AdjustStockResponseDto
        {
            Product = product,
            PreviousQuantity = previousQuantity,
            CurrentQuantity = product.Quantity
        });
    }

    [HttpPost("release")]
    [AllowAnonymous]
    public async Task<IActionResult> ReleaseStock(AdjustStockDto dto)
    {
        if (!HasValidInternalApiKey())
            return Unauthorized(new { message = "Internal API key không hợp lệ" });

        if (dto.ProductId <= 0 || dto.Quantity <= 0)
            return BadRequest(new { message = "ProductId và số lượng phải lớn hơn 0" });

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var affected = await _context.Inventories
            .Where(inventory => inventory.ProductId == dto.ProductId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    inventory => inventory.Quantity,
                    inventory => inventory.Quantity + dto.Quantity));

        if (affected == 0)
            return NotFound(new { message = "Không tìm thấy sản phẩm" });

        var product = await LoadProductStockAsync(dto.ProductId);
        var previousQuantity = product.Quantity - dto.Quantity;
        _context.StockEvents.Add(CreateStockEvent(
            product,
            previousQuantity,
            "order.released",
            dto.ReferenceId));
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new AdjustStockResponseDto
        {
            Product = product,
            PreviousQuantity = previousQuantity,
            CurrentQuantity = product.Quantity
        });
    }

    private async Task<ProductStockDto> LoadProductStockAsync(int productId)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => new ProductStockDto
            {
                Id = product.Id,
                Name = product.Name,
                SellingPrice = product.SellingPrice,
                CategoryName = product.Category.Name,
                Quantity = product.Inventory.Quantity,
                ReserveStock = product.Inventory.ReserveStock
            })
            .SingleAsync();
    }

    private void AddStockEvent(
        Inventory inventory,
        int previousQuantity,
        string source,
        string? referenceId)
    {
        _context.StockEvents.Add(new StockEvent
        {
            ProductId = inventory.ProductId,
            ProductName = inventory.Product.Name,
            PreviousQuantity = previousQuantity,
            CurrentQuantity = inventory.Quantity,
            QuantityChange = inventory.Quantity - previousQuantity,
            Source = source,
            ReferenceId = referenceId
        });
    }

    private static StockEvent CreateStockEvent(
        ProductStockDto product,
        int previousQuantity,
        string source,
        string? referenceId)
    {
        return new StockEvent
        {
            ProductId = product.Id,
            ProductName = product.Name,
            PreviousQuantity = previousQuantity,
            CurrentQuantity = product.Quantity,
            QuantityChange = product.Quantity - previousQuantity,
            Source = source,
            ReferenceId = referenceId
        };
    }

    private bool HasValidInternalApiKey()
    {
        var expected = _configuration["Services:InternalApiKey"];
        var actual = Request.Headers["X-Internal-Api-Key"].ToString();
        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(expected, actual, StringComparison.Ordinal);
    }
}

using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhom1_sales_and_inventory_management.Domain.Entities;
using nhom1_sales_and_inventory_management.DTOs.Inventory;
using nhom1_sales_and_inventory_management.Infrastructure.Data;
using nhom1_sales_and_inventory_management.Services;

namespace nhom1_sales_and_inventory_management.Controllers;

[ApiController]
[Route("api/stock-receipts")]
[Authorize(Roles = "Admin,WarehouseKeeper")]
public class StockReceiptController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISupplierClient _supplierClient;

    public StockReceiptController(ApplicationDbContext context, ISupplierClient supplierClient)
    {
        _context = context;
        _supplierClient = supplierClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var receipts = await _context.StockReceipts
            .AsNoTracking()
            .Include(receipt => receipt.Items)
            .ThenInclude(item => item.Product)
            .OrderByDescending(receipt => receipt.CreatedAt)
            .ToListAsync();

        return Ok(receipts.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var receipt = await LoadReceiptAsync(id);
        return receipt is null ? NotFound() : Ok(Map(receipt));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStockReceiptDto dto)
    {
        if (dto.SupplierId <= 0)
            return BadRequest(new { message = "SupplierId phải lớn hơn 0" });

        if (dto.Items.Count == 0
            || dto.Items.Any(item => item.ProductId <= 0
                || item.Quantity <= 0
                || item.ImportPrice < 0))
        {
            return BadRequest(new { message = "Phiếu nhập phải có sản phẩm hợp lệ" });
        }

        var supplier = await _supplierClient.GetByIdAsync(dto.SupplierId);
        if (supplier is null)
            return NotFound(new { message = "Không tìm thấy nhà cung cấp" });

        var items = dto.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new StockReceiptItem
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity),
                ImportPrice = group.Last().ImportPrice
            })
            .ToList();
        var productIds = items.Select(item => item.ProductId).ToList();
        var existingProductIds = await _context.Products
            .Where(product => productIds.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync();

        if (existingProductIds.Count != productIds.Count)
            return NotFound(new { message = "Một hoặc nhiều sản phẩm không tồn tại" });

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userIdValue, out var createdByUserId))
            return Unauthorized(new { message = "JWT không chứa UserId hợp lệ" });

        var receipt = new StockReceipt
        {
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            CreatedByUserId = createdByUserId,
            Items = items
        };

        _context.StockReceipts.Add(receipt);
        await _context.SaveChangesAsync();
        receipt = (await LoadReceiptAsync(receipt.Id))!;

        return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, Map(receipt));
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);
        var receipt = await _context.StockReceipts
            .Include(value => value.Items)
            .ThenInclude(item => item.Product)
            .ThenInclude(product => product.Inventory)
            .FirstOrDefaultAsync(value => value.Id == id);

        if (receipt is null)
            return NotFound();

        if (receipt.Status != StockReceiptStatus.Draft)
            return Conflict(new { message = "Chỉ có thể xác nhận phiếu nhập Draft" });

        foreach (var item in receipt.Items)
        {
            var inventory = item.Product.Inventory;
            var previousQuantity = inventory.Quantity;
            inventory.Quantity += item.Quantity;
            item.Product.ImportPrice = item.ImportPrice;
            _context.StockEvents.Add(new StockEvent
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                PreviousQuantity = previousQuantity,
                CurrentQuantity = inventory.Quantity,
                QuantityChange = item.Quantity,
                Source = "stock.receipt.confirmed",
                ReferenceId = receipt.Id.ToString()
            });
        }

        receipt.Status = StockReceiptStatus.Confirmed;
        receipt.ConfirmedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(Map(receipt));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var receipt = await _context.StockReceipts.FindAsync(id);
        if (receipt is null)
            return NotFound();

        if (receipt.Status != StockReceiptStatus.Draft)
            return Conflict(new { message = "Chỉ có thể hủy phiếu nhập Draft" });

        receipt.Status = StockReceiptStatus.Cancelled;
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    private Task<StockReceipt?> LoadReceiptAsync(int id)
    {
        return _context.StockReceipts
            .AsNoTracking()
            .Include(receipt => receipt.Items)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(receipt => receipt.Id == id);
    }

    private static StockReceiptResponseDto Map(StockReceipt receipt)
    {
        return new StockReceiptResponseDto
        {
            Id = receipt.Id,
            SupplierId = receipt.SupplierId,
            SupplierName = receipt.SupplierName,
            Note = receipt.Note,
            Status = receipt.Status.ToString(),
            CreatedAt = receipt.CreatedAt,
            ConfirmedAt = receipt.ConfirmedAt,
            CreatedByUserId = receipt.CreatedByUserId,
            Items = receipt.Items.Select(item => new StockReceiptItemResponseDto
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                Quantity = item.Quantity,
                ImportPrice = item.ImportPrice
            }).ToList()
        };
    }
}

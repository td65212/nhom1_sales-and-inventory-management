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
    [Authorize(Roles = "Admin,WarehouseKeeper")]
    public async Task<IActionResult> Create(CreateStockReceiptDto dto)
    {
        var validationError = await ValidateCreateAsync(dto);
        if (validationError is not null)
            return validationError;

        var supplier = await _supplierClient.GetByIdAsync(dto.SupplierId);
        if (supplier is null)
            return NotFound(new { message = "Khong tim thay nha cung cap" });

        var createdByUserId = GetCurrentUserId();
        if (createdByUserId is null)
            return Unauthorized(new { message = "JWT khong chua UserId hop le" });

        var items = dto.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new StockReceiptItem
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity),
                ImportPrice = group.Last().ImportPrice
            })
            .ToList();

        var receipt = new StockReceipt
        {
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            InvoiceNumber = string.IsNullOrWhiteSpace(dto.InvoiceNumber)
                ? $"PN-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : dto.InvoiceNumber.Trim(),
            ImportDate = (dto.ImportDate ?? DateTime.UtcNow).Date,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            CreatedByUserId = createdByUserId.Value,
            Items = items
        };

        _context.StockReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, Map((await LoadReceiptAsync(receipt.Id))!));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseKeeper")]
    public async Task<IActionResult> Update(int id, CreateStockReceiptDto dto)
    {
        var receipt = await _context.StockReceipts
            .Include(value => value.Items)
            .FirstOrDefaultAsync(value => value.Id == id);
        if (receipt is null)
            return NotFound();
        if (receipt.Status != StockReceiptStatus.Draft)
            return Conflict(new { message = "Chi co the sua phieu Draft" });

        var validationError = await ValidateCreateAsync(dto);
        if (validationError is not null)
            return validationError;

        var supplier = await _supplierClient.GetByIdAsync(dto.SupplierId);
        if (supplier is null)
            return NotFound(new { message = "Khong tim thay nha cung cap" });

        receipt.SupplierId = supplier.Id;
        receipt.SupplierName = supplier.Name;
        receipt.InvoiceNumber = string.IsNullOrWhiteSpace(dto.InvoiceNumber)
            ? receipt.InvoiceNumber
            : dto.InvoiceNumber.Trim();
        receipt.ImportDate = (dto.ImportDate ?? receipt.ImportDate).Date;
        receipt.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
        receipt.Items.Clear();
        foreach (var item in dto.Items
            .GroupBy(value => value.ProductId)
            .Select(group => new StockReceiptItem
            {
                ProductId = group.Key,
                Quantity = group.Sum(value => value.Quantity),
                ImportPrice = group.Last().ImportPrice
            }))
        {
            receipt.Items.Add(item);
        }

        await _context.SaveChangesAsync();
        return Ok(Map((await LoadReceiptAsync(id))!));
    }

    [HttpPost("{id:int}/submit")]
    [Authorize(Roles = "Admin,WarehouseKeeper")]
    public async Task<IActionResult> Submit(int id)
    {
        var receipt = await _context.StockReceipts.FindAsync(id);
        if (receipt is null)
            return NotFound();
        if (receipt.Status != StockReceiptStatus.Draft)
            return Conflict(new { message = "Chi co the gui duyet phieu Draft" });

        receipt.Status = StockReceiptStatus.PendingApproval;
        receipt.SubmittedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(Map((await LoadReceiptAsync(id))!));
    }

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = "Admin")]
    public Task<IActionResult> Confirm(int id) => Approve(id);

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var receipt = await _context.StockReceipts
            .Include(value => value.Items)
            .ThenInclude(item => item.Product)
            .ThenInclude(product => product.Inventory)
            .FirstOrDefaultAsync(value => value.Id == id);

        if (receipt is null)
            return NotFound();
        if (receipt.Status == StockReceiptStatus.Approved)
        {
            await transaction.CommitAsync();
            return Ok(Map(receipt));
        }
        if (receipt.Status != StockReceiptStatus.PendingApproval)
            return Conflict(new { message = "Chi co the duyet phieu PendingApproval" });

        var approvedByUserId = GetCurrentUserId();
        if (approvedByUserId is null)
            return Unauthorized(new { message = "JWT khong chua UserId hop le" });

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
                Source = "stock.receipt.approved",
                ReferenceId = receipt.Id.ToString()
            });
        }

        receipt.Status = StockReceiptStatus.Approved;
        receipt.ApprovedByUserId = approvedByUserId.Value;
        receipt.ApprovedAt = DateTime.UtcNow;
        receipt.ConfirmedAt = receipt.ApprovedAt;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(Map(receipt));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Admin")]
    public Task<IActionResult> Cancel(int id) => Reject(id);

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id)
    {
        var receipt = await _context.StockReceipts.FindAsync(id);
        if (receipt is null)
            return NotFound();
        if (receipt.Status != StockReceiptStatus.PendingApproval)
            return Conflict(new { message = "Chi co the tu choi phieu PendingApproval" });

        receipt.Status = StockReceiptStatus.Rejected;
        receipt.ApprovedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    private async Task<IActionResult?> ValidateCreateAsync(CreateStockReceiptDto dto)
    {
        if (dto.SupplierId <= 0)
            return BadRequest(new { message = "SupplierId phai lon hon 0" });

        if (dto.Items.Count == 0
            || dto.Items.Any(item => item.ProductId <= 0 || item.Quantity <= 0 || item.ImportPrice < 0))
            return BadRequest(new { message = "Phieu nhap phai co san pham hop le" });

        var productIds = dto.Items.Select(item => item.ProductId).Distinct().ToList();
        var existingProductIds = await _context.Products
            .Where(product => productIds.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync();
        return existingProductIds.Count == productIds.Count
            ? null
            : NotFound(new { message = "Mot hoac nhieu san pham khong ton tai" });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var userId) ? userId : null;
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
            InvoiceNumber = receipt.InvoiceNumber,
            ImportDate = receipt.ImportDate,
            Note = receipt.Note,
            Status = receipt.Status.ToString(),
            CreatedAt = receipt.CreatedAt,
            SubmittedAt = receipt.SubmittedAt,
            ApprovedAt = receipt.ApprovedAt,
            ConfirmedAt = receipt.ConfirmedAt,
            CreatedByUserId = receipt.CreatedByUserId,
            ApprovedByUserId = receipt.ApprovedByUserId,
            TotalAmount = receipt.Items.Sum(item => item.Quantity * item.ImportPrice),
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

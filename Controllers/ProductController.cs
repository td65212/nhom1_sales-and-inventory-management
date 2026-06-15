using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhom1_sales_and_inventory_management.Domain.Entities;
using nhom1_sales_and_inventory_management.DTOs.Product;
using nhom1_sales_and_inventory_management.Infrastructure.Data;
using nhom1_sales_and_inventory_management.Services;

namespace nhom1_sales_and_inventory_management.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISupplierClient _supplierClient;

    public ProductController(ApplicationDbContext context, ISupplierClient supplierClient)
    {
        _context = context;
        _supplierClient = supplierClient;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Inventory)
            .OrderBy(product => product.Id)
            .ToListAsync();
        var suppliers = await _supplierClient.GetByIdsAsync(
            products.Select(product => product.SupplierId));

        return Ok(products.Select(product =>
            Map(product, suppliers.GetValueOrDefault(product.SupplierId)?.Name)));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await LoadProductAsync(id);
        if (product is null)
            return NotFound();

        var supplier = await _supplierClient.GetByIdAsync(product.SupplierId);
        return Ok(Map(product, supplier?.Name));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseKeeper")]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var validationResult = await ValidateAsync(dto.Name, dto.ImportPrice, dto.SellingPrice,
            dto.CategoryId, dto.SupplierId, dto.Quantity, dto.ReserveStock);
        if (validationResult.Error is not null)
            return validationResult.Error;

        var product = new Product
        {
            Name = dto.Name.Trim(),
            ImportPrice = dto.ImportPrice,
            SellingPrice = dto.SellingPrice,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            SupplierId = validationResult.Supplier!.Id,
            Inventory = new Inventory
            {
                Quantity = dto.Quantity,
                ReserveStock = dto.ReserveStock
            }
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        AddStockEvent(product, 0, product.Inventory.Quantity, "product.created");
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            Map((await LoadProductAsync(product.Id))!, validationResult.Supplier.Name));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseKeeper")]
    public async Task<IActionResult> Update(int id, UpdateProductDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { message = "ID khong khop" });

        var product = await _context.Products
            .Include(value => value.Inventory)
            .FirstOrDefaultAsync(value => value.Id == id);
        if (product is null)
            return NotFound();

        var validationResult = await ValidateAsync(dto.Name, dto.ImportPrice, dto.SellingPrice,
            dto.CategoryId, dto.SupplierId, dto.Quantity, dto.ReserveStock);
        if (validationResult.Error is not null)
            return validationResult.Error;

        var previousQuantity = product.Inventory.Quantity;
        product.Name = dto.Name.Trim();
        product.ImportPrice = dto.ImportPrice;
        product.SellingPrice = dto.SellingPrice;
        product.ImageUrl = dto.ImageUrl;
        product.CategoryId = dto.CategoryId;
        product.SupplierId = validationResult.Supplier!.Id;
        product.Inventory.Quantity = dto.Quantity;
        product.Inventory.ReserveStock = dto.ReserveStock;

        if (previousQuantity != product.Inventory.Quantity)
            AddStockEvent(product, previousQuantity, product.Inventory.Quantity, "product.updated");

        await _context.SaveChangesAsync();
        return Ok(Map((await LoadProductAsync(product.Id))!, validationResult.Supplier.Name));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return Ok();
    }

    private async Task<(DTOs.Integration.SupplierDto? Supplier, IActionResult? Error)> ValidateAsync(
        string name, decimal importPrice, decimal sellingPrice, int categoryId,
        int supplierId, int quantity, int reserveStock)
    {
        if (string.IsNullOrWhiteSpace(name) || importPrice < 0 || sellingPrice < 0
            || categoryId <= 0 || supplierId <= 0 || quantity < 0 || reserveStock < 0)
        {
            return (null, BadRequest(new { message = "Du lieu san pham khong hop le" }));
        }

        if (!await _context.Categories.AnyAsync(category => category.Id == categoryId))
            return (null, NotFound(new { message = "Khong tim thay danh muc" }));

        var supplier = await _supplierClient.GetByIdAsync(supplierId);
        return supplier is null
            ? (null, NotFound(new { message = "Khong tim thay nha cung cap" }))
            : (supplier, null);
    }

    private Task<Product?> LoadProductAsync(int id)
    {
        return _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Inventory)
            .FirstOrDefaultAsync(product => product.Id == id);
    }

    private void AddStockEvent(
        Product product, int previousQuantity, int currentQuantity, string source)
    {
        _context.StockEvents.Add(new StockEvent
        {
            ProductId = product.Id,
            ProductName = product.Name,
            PreviousQuantity = previousQuantity,
            CurrentQuantity = currentQuantity,
            QuantityChange = currentQuantity - previousQuantity,
            Source = source,
            ReferenceId = product.Id.ToString()
        });
    }

    private static ProductResponseDto Map(Product product, string? supplierName)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            ImportPrice = product.ImportPrice,
            SellingPrice = product.SellingPrice,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            SupplierId = product.SupplierId,
            SupplierName = supplierName ?? string.Empty,
            Quantity = product.Inventory.Quantity,
            ReserveStock = product.Inventory.ReserveStock
        };
    }
}

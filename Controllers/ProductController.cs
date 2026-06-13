using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhom1_sales_and_inventory_management.Domain.Entities;
using nhom1_sales_and_inventory_management.DTOs.Product;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

namespace nhom1_sales_and_inventory_management.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                ImportPrice = p.ImportPrice,
                SellingPrice = p.SellingPrice,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                Quantity = p.Inventory.Quantity,
                ReserveStock = p.Inventory.ReserveStock,
                CategoryName = p.Category.Name
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _context.Products
            .Include(x => x.Category)
            .Include(x => x.Inventory)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null)
            return NotFound();

        return Ok(new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            ImportPrice = p.ImportPrice,
            SellingPrice = p.SellingPrice,
            ImageUrl = p.ImageUrl,
            CategoryId = p.CategoryId,
            Quantity = p.Inventory.Quantity,
            ReserveStock = p.Inventory.ReserveStock,
            CategoryName = p.Category.Name
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseKeeper")]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)
            || dto.ImportPrice < 0
            || dto.SellingPrice < 0
            || dto.Quantity < 0
            || dto.ReserveStock < 0)
        {
            return BadRequest(new { message = "Dữ liệu sản phẩm không hợp lệ" });
        }

        if (!await _context.Categories.AnyAsync(category => category.Id == dto.CategoryId))
            return NotFound(new { message = "Không tìm thấy danh mục" });

        var product = new Product
        {
            Name = dto.Name.Trim(),
            ImportPrice = dto.ImportPrice,
            SellingPrice = dto.SellingPrice,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            Inventory = new Inventory
            {
                Quantity = dto.Quantity,
                ReserveStock = dto.ReserveStock
            }
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        _context.StockEvents.Add(new StockEvent
        {
            ProductId = product.Id,
            ProductName = product.Name,
            PreviousQuantity = 0,
            CurrentQuantity = product.Inventory.Quantity,
            QuantityChange = product.Inventory.Quantity,
            Source = "product.created",
            ReferenceId = product.Id.ToString()
        });
        await _context.SaveChangesAsync();

        var response = await GetResponseAsync(product.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = product.Id },
            response);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,WarehouseKeeper")]
    public async Task<IActionResult> Update(
        int id,
        UpdateProductDto dto)
    {
        if (id != dto.Id)
            return BadRequest();

        var product = await _context.Products
            .Include(x => x.Inventory)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return NotFound();

        if (dto.ImportPrice < 0
            || dto.SellingPrice < 0
            || dto.Quantity < 0
            || dto.ReserveStock < 0)
        {
            return BadRequest(new { message = "Dữ liệu sản phẩm không hợp lệ" });
        }

        var previousQuantity = product.Inventory.Quantity;
        product.Name = dto.Name.Trim();
        product.ImportPrice = dto.ImportPrice;
        product.SellingPrice = dto.SellingPrice;
        product.ImageUrl = dto.ImageUrl;
        product.CategoryId = dto.CategoryId;

        product.Inventory.Quantity = dto.Quantity;
        product.Inventory.ReserveStock = dto.ReserveStock;

        if (previousQuantity != product.Inventory.Quantity)
        {
            _context.StockEvents.Add(new StockEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                PreviousQuantity = previousQuantity,
                CurrentQuantity = product.Inventory.Quantity,
                QuantityChange = product.Inventory.Quantity - previousQuantity,
                Source = "product.updated",
                ReferenceId = product.Id.ToString()
            });
        }

        await _context.SaveChangesAsync();

        return Ok(await GetResponseAsync(product.Id));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        _context.Products.Remove(product);

        await _context.SaveChangesAsync();

        return Ok();
    }

    private Task<ProductResponseDto> GetResponseAsync(int id)
    {
        return _context.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                ImportPrice = product.ImportPrice,
                SellingPrice = product.SellingPrice,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                Quantity = product.Inventory.Quantity,
                ReserveStock = product.Inventory.ReserveStock
            })
            .SingleAsync();
    }
}

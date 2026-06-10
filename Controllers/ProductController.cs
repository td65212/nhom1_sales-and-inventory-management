using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhom1_sales_and_inventory_management.Domain.Entities;
using nhom1_sales_and_inventory_management.DTOs.Product;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

namespace nhom1_sales_and_inventory_management.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                Name = p.Name,
                SellingPrice = p.SellingPrice,
                Quantity = p.Inventory.Quantity,
                ReserveStock = p.Inventory.ReserveStock,
                CategoryName = p.Category.Name
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
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
            ProductCode = p.ProductCode,
            Name = p.Name,
            SellingPrice = p.SellingPrice,
            Quantity = p.Inventory.Quantity,
            ReserveStock = p.Inventory.ReserveStock,
            CategoryName = p.Category.Name
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var product = new Product
        {
            ProductCode = dto.ProductCode,
            Name = dto.Name,
            ImportPrice = dto.ImportPrice,
            SellingPrice = dto.SellingPrice,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var inventory = new Inventory
{
    ProductId = product.Id,
    Quantity = dto.Quantity,
    ReserveStock = dto.ReserveStock
};

        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = product.Id },
            product);
    }

    [HttpPut("{id}")]
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

        product.ProductCode = dto.ProductCode;
        product.Name = dto.Name;
        product.ImportPrice = dto.ImportPrice;
        product.SellingPrice = dto.SellingPrice;
        product.ImageUrl = dto.ImageUrl;
        product.CategoryId = dto.CategoryId;

        product.Inventory.Quantity = dto.Quantity;
        product.Inventory.ReserveStock = dto.ReserveStock;

        await _context.SaveChangesAsync();

        return Ok(product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        _context.Products.Remove(product);

        await _context.SaveChangesAsync();

        return Ok();
    }
}
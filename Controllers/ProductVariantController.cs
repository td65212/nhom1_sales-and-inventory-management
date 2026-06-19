using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhom1_sales_and_inventory_management.Domain.Entities;
using nhom1_sales_and_inventory_management.DTOs.Product;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

namespace nhom1_sales_and_inventory_management.Controllers;

[ApiController]
[Route("api/product-variants")]
[Authorize(Roles = "Admin,WarehouseKeeper")]
public class ProductVariantController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public ProductVariantController(ApplicationDbContext context) => _context = context;

    [HttpPost("product/{productId:int}")]
    public async Task<IActionResult> CreateVariant(int productId, UpsertProductVariantDto dto)
    {
        var error = Validate(dto);
        if (error is not null) return BadRequest(new { message = error });
        if (!await _context.Products.AnyAsync(product => product.Id == productId)) return NotFound();
        if (await _context.ProductVariants.AnyAsync(variant => variant.Sku == dto.Sku.Trim()))
            return Conflict(new { message = "SKU đã tồn tại" });
        var variant = Apply(new ProductVariant { ProductId = productId }, dto);
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        return Ok(ProductController.MapVariant(variant));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateVariant(int id, UpsertProductVariantDto dto)
    {
        var error = Validate(dto);
        if (error is not null) return BadRequest(new { message = error });
        var variant = await _context.ProductVariants.Include(value => value.Colors)
            .ThenInclude(color => color.Images).FirstOrDefaultAsync(value => value.Id == id);
        if (variant is null) return NotFound();
        if (await _context.ProductVariants.AnyAsync(value => value.Id != id && value.Sku == dto.Sku.Trim()))
            return Conflict(new { message = "SKU đã tồn tại" });
        Apply(variant, dto);
        await _context.SaveChangesAsync();
        return Ok(ProductController.MapVariant(variant));
    }

    [HttpPost("{variantId:int}/colors")]
    public async Task<IActionResult> CreateColor(int variantId, UpsertProductVariantColorDto dto)
    {
        var error = Validate(dto);
        if (error is not null) return BadRequest(new { message = error });
        if (!await _context.ProductVariants.AnyAsync(variant => variant.Id == variantId)) return NotFound();
        if (await _context.ProductVariantColors.AnyAsync(color => color.ProductVariantId == variantId && color.Name == dto.Name.Trim()))
            return Conflict(new { message = "Màu đã tồn tại trong phiên bản" });
        var color = Apply(new ProductVariantColor { ProductVariantId = variantId }, dto);
        _context.ProductVariantColors.Add(color);
        await _context.SaveChangesAsync();
        return Ok(color);
    }

    [HttpPut("colors/{id:int}")]
    public async Task<IActionResult> UpdateColor(int id, UpsertProductVariantColorDto dto)
    {
        var error = Validate(dto);
        if (error is not null) return BadRequest(new { message = error });
        var color = await _context.ProductVariantColors.Include(value => value.Images)
            .FirstOrDefaultAsync(value => value.Id == id);
        if (color is null) return NotFound();
        if (await _context.ProductVariantColors.AnyAsync(value => value.Id != id
            && value.ProductVariantId == color.ProductVariantId && value.Name == dto.Name.Trim()))
            return Conflict(new { message = "Màu đã tồn tại trong phiên bản" });
        Apply(color, dto);
        await _context.SaveChangesAsync();
        return Ok(color);
    }

    private static ProductVariant Apply(ProductVariant entity, UpsertProductVariantDto dto)
    {
        entity.Name = dto.Name.Trim(); entity.Sku = dto.Sku.Trim();
        entity.OriginalPrice = dto.OriginalPrice;
        entity.SalePrice = dto.SalePrice is > 0 && dto.SalePrice < dto.OriginalPrice ? dto.SalePrice : null;
        entity.Quantity = dto.Quantity; entity.ReserveStock = dto.ReserveStock; entity.IsActive = dto.IsActive;
        return entity;
    }

    private static ProductVariantColor Apply(ProductVariantColor entity, UpsertProductVariantColorDto dto)
    {
        entity.Name = dto.Name.Trim(); entity.HexCode = string.IsNullOrWhiteSpace(dto.HexCode) ? null : dto.HexCode.Trim();
        entity.Quantity = dto.Quantity; entity.IsActive = dto.IsActive;
        entity.Images.Clear();
        foreach (var image in dto.ImageUrls.Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(12)
            .Select((url, index) => new ProductImage { ImageUrl = url, SortOrder = index })) entity.Images.Add(image);
        return entity;
    }

    private static string? Validate(UpsertProductVariantDto dto) =>
        string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Sku) || dto.OriginalPrice < 0
        || dto.Quantity < 0 || dto.ReserveStock < 0 ? "Dữ liệu phiên bản không hợp lệ" : null;
    private static string? Validate(UpsertProductVariantColorDto dto) =>
        string.IsNullOrWhiteSpace(dto.Name) || dto.Quantity < 0 ? "Dữ liệu màu sắc không hợp lệ" : null;
}

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
        var products = await ProductQuery().OrderBy(product => product.Id).ToListAsync();
        var suppliers = await _supplierClient.GetByIdsAsync(products.Select(product => product.SupplierId));
        return Ok(products.Select(product => Map(product, suppliers.GetValueOrDefault(product.SupplierId)?.Name)));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await ProductQuery().FirstOrDefaultAsync(value => value.Id == id);
        if (product is null)
            return NotFound();

        var supplier = await _supplierClient.GetByIdAsync(product.SupplierId);
        return Ok(Map(product, supplier?.Name));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseKeeper")]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var validation = await ValidateAsync(dto.Name, dto.ImportPrice, dto.SellingPrice,
            dto.CategoryId, dto.SupplierId, dto.Quantity, dto.ReserveStock);
        if (validation.Error is not null)
            return validation.Error;

        var originalPrice = dto.OriginalPrice ?? dto.SellingPrice;
        var salePrice = NormalizeSalePrice(originalPrice, dto.SalePrice);
        var product = new Product
        {
            Name = dto.Name.Trim(),
            Description = Normalize(dto.Description),
            ImportPrice = dto.ImportPrice,
            OriginalPrice = originalPrice,
            SalePrice = salePrice,
            SellingPrice = salePrice ?? originalPrice,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            SupplierId = validation.Supplier!.Id,
            Inventory = new Inventory { Quantity = dto.Quantity, ReserveStock = dto.ReserveStock }
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var color = new ProductVariantColor
        {
            Name = "Mặc định",
            Quantity = dto.Quantity,
            Images = NormalizeImageUrls(dto.ImageItems.Select(item => item.ImageUrl).Concat(dto.ImageUrls))
                .Select((url, index) => new ProductImage { ImageUrl = url, SortOrder = index })
                .ToList()
        };
        product.Variants.Add(new ProductVariant
        {
            Name = "Mặc định",
            Sku = $"SP-{product.Id}-DEFAULT",
            OriginalPrice = originalPrice,
            SalePrice = salePrice,
            Quantity = dto.Quantity,
            ReserveStock = dto.ReserveStock,
            Colors = new List<ProductVariantColor> { color }
        });
        AddStockEvent(product, 0, dto.Quantity, "product.created");
        await _context.SaveChangesAsync();

        var created = await ProductQuery().SingleAsync(value => value.Id == product.Id);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, Map(created, validation.Supplier.Name));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseKeeper")]
    public async Task<IActionResult> Update(int id, UpdateProductDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { message = "ID không khớp" });

        var product = await _context.Products.Include(value => value.Inventory)
            .FirstOrDefaultAsync(value => value.Id == id);
        if (product is null)
            return NotFound();

        var validation = await ValidateAsync(dto.Name, dto.ImportPrice, dto.SellingPrice,
            dto.CategoryId, dto.SupplierId, dto.Quantity, dto.ReserveStock);
        if (validation.Error is not null)
            return validation.Error;

        product.Name = dto.Name.Trim();
        product.Description = Normalize(dto.Description);
        product.ImportPrice = dto.ImportPrice;
        product.OriginalPrice = dto.OriginalPrice ?? dto.SellingPrice;
        product.SalePrice = NormalizeSalePrice(product.OriginalPrice, dto.SalePrice);
        product.SellingPrice = product.SalePrice ?? product.OriginalPrice;
        product.ImageUrl = dto.ImageUrl;
        product.CategoryId = dto.CategoryId;
        product.SupplierId = validation.Supplier!.Id;
        product.Inventory.ReserveStock = dto.ReserveStock;
        await _context.SaveChangesAsync();

        var updated = await ProductQuery().SingleAsync(value => value.Id == id);
        return Ok(Map(updated, validation.Supplier.Name));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,WarehouseKeeper")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<Product> ProductQuery() => _context.Products.AsNoTracking()
        .Include(product => product.Category)
        .Include(product => product.Inventory)
        .Include(product => product.Variants).ThenInclude(variant => variant.Colors)
        .ThenInclude(color => color.Images);

    private async Task<(DTOs.Integration.SupplierDto? Supplier, IActionResult? Error)> ValidateAsync(
        string name, decimal importPrice, decimal sellingPrice, int categoryId,
        int supplierId, int quantity, int reserveStock)
    {
        if (string.IsNullOrWhiteSpace(name) || importPrice < 0 || sellingPrice < 0
            || categoryId <= 0 || supplierId <= 0 || quantity < 0 || reserveStock < 0)
            return (null, BadRequest(new { message = "Dữ liệu sản phẩm không hợp lệ" }));
        if (!await _context.Categories.AnyAsync(category => category.Id == categoryId))
            return (null, NotFound(new { message = "Không tìm thấy danh mục" }));
        var supplier = await _supplierClient.GetByIdAsync(supplierId);
        return supplier is null
            ? (null, NotFound(new { message = "Không tìm thấy nhà cung cấp" }))
            : (supplier, null);
    }

    internal static ProductResponseDto Map(Product product, string? supplierName)
    {
        var variants = product.Variants.OrderBy(variant => variant.Id).Select(MapVariant).ToList();
        var activeVariants = variants.Where(variant => variant.IsActive).ToList();
        var primary = activeVariants.OrderBy(variant => variant.SellingPrice).FirstOrDefault();
        var images = variants.SelectMany(variant => variant.Colors)
            .SelectMany(color => color.Images).OrderBy(image => image.SortOrder).ToList();
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            ImportPrice = product.ImportPrice,
            OriginalPrice = primary?.OriginalPrice ?? product.OriginalPrice,
            SalePrice = primary?.SalePrice ?? product.SalePrice,
            SellingPrice = primary?.SellingPrice ?? product.SellingPrice,
            ImageUrl = product.ImageUrl ?? images.FirstOrDefault()?.ImageUrl,
            ImageUrls = images.Select(image => image.ImageUrl).Distinct().ToList(),
            ImageItems = images,
            Variants = variants,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            SupplierId = product.SupplierId,
            SupplierName = supplierName ?? string.Empty,
            Quantity = activeVariants.Sum(variant => variant.Quantity),
            ReserveStock = activeVariants.Sum(variant => variant.ReserveStock)
        };
    }

    internal static ProductVariantDto MapVariant(ProductVariant variant) => new()
    {
        Id = variant.Id,
        ProductId = variant.ProductId,
        Name = variant.Name,
        Sku = variant.Sku,
        OriginalPrice = variant.OriginalPrice,
        SalePrice = NormalizeSalePrice(variant.OriginalPrice, variant.SalePrice),
        SellingPrice = NormalizeSalePrice(variant.OriginalPrice, variant.SalePrice) ?? variant.OriginalPrice,
        Quantity = variant.Quantity,
        ReserveStock = variant.ReserveStock,
        IsActive = variant.IsActive,
        Colors = variant.Colors.OrderBy(color => color.Id).Select(color => new ProductVariantColorDto
        {
            Id = color.Id,
            Name = color.Name,
            HexCode = color.HexCode,
            Quantity = color.Quantity,
            IsActive = color.IsActive,
            Images = color.Images.OrderBy(image => image.SortOrder).Select(image => new ProductImageItemDto
            {
                Id = image.Id,
                ImageUrl = image.ImageUrl,
                SortOrder = image.SortOrder
            }).ToList()
        }).ToList()
    };

    private static decimal? NormalizeSalePrice(decimal originalPrice, decimal? salePrice) =>
        salePrice is > 0 && salePrice < originalPrice ? salePrice : null;
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static IEnumerable<string> NormalizeImageUrls(IEnumerable<string?> urls) => urls
        .Where(url => !string.IsNullOrWhiteSpace(url)).Select(url => url!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase).Take(12);

    private void AddStockEvent(Product product, int previousQuantity, int currentQuantity, string source) =>
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

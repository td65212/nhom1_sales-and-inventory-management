namespace nhom1_sales_and_inventory_management.DTOs.Category;

public class CategoryTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public List<CategoryTreeDto> Children { get; set; } = new();
}

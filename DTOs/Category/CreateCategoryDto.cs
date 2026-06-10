namespace nhom1_sales_and_inventory_management.DTOs.Category;

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;

    public int? ParentCategoryId { get; set; }
}
namespace nhom1_sales_and_inventory_management.DTOs.Category;

public class UpdateCategoryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? ParentCategoryId { get; set; }
}
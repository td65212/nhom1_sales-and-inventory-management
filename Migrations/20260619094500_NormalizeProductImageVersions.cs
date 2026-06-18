using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

#nullable disable

namespace nhom1_sales_and_inventory_management.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260619094500_NormalizeProductImageVersions")]
public partial class NormalizeProductImageVersions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "ProductImages"
            SET "Version" = 'v1.' || (COALESCE("SortOrder", 0) + 1)::text
            WHERE "Version" IS NULL
               OR btrim("Version") = ''
               OR "Version" IN ('Ảnh chính', 'Góc nghiêng', 'Chi tiết sản phẩm', 'Phiên bản thực tế', 'Phiên bản bổ sung');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "ProductImages"
            SET "Version" = NULL
            WHERE "Version" LIKE 'v1.%';
            """);
    }
}

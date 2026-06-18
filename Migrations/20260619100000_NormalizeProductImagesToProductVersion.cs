using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

#nullable disable

namespace nhom1_sales_and_inventory_management.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260619100000_NormalizeProductImagesToProductVersion")]
public partial class NormalizeProductImagesToProductVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH product_versions AS (
                SELECT "ProductId", MIN(NULLIF(btrim("Version"), '')) AS "ProductVersion"
                FROM "ProductImages"
                GROUP BY "ProductId"
            )
            UPDATE "ProductImages" AS image
            SET "Version" = COALESCE(product_versions."ProductVersion", 'v1.1')
            FROM product_versions
            WHERE image."ProductId" = product_versions."ProductId";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

#nullable disable

namespace nhom1_sales_and_inventory_management.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260619093000_AddProductImageVersion")]
public partial class AddProductImageVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Version",
            table: "ProductImages",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE "ProductImages"
            SET "Version" = 'v1.' || (COALESCE("SortOrder", 0) + 1)::text
            WHERE "Version" IS NULL OR btrim("Version") = '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Version",
            table: "ProductImages");
    }
}

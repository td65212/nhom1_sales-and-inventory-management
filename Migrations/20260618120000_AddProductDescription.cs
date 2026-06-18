using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

#nullable disable

namespace nhom1_salesandinventorymanagement.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260618120000_AddProductDescription")]
public class AddProductDescription : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "ALTER TABLE \"Products\" ADD COLUMN IF NOT EXISTS \"Description\" text;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "ALTER TABLE \"Products\" DROP COLUMN IF EXISTS \"Description\";");
    }
}

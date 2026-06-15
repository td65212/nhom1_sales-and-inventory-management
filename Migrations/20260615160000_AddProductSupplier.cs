using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

#nullable disable

namespace nhom1_salesandinventorymanagement.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260615160000_AddProductSupplier")]
public class AddProductSupplier : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SupplierId",
            table: "Products",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateIndex(
            name: "IX_Products_SupplierId",
            table: "Products",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_StockEvents_ProductId_Source_ReferenceId",
            table: "StockEvents",
            columns: new[] { "ProductId", "Source", "ReferenceId" },
            unique: true,
            filter: "\"ReferenceId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_StockEvents_ProductId_Source_ReferenceId",
            table: "StockEvents");

        migrationBuilder.DropIndex(
            name: "IX_Products_SupplierId",
            table: "Products");
        migrationBuilder.DropColumn(
            name: "SupplierId",
            table: "Products");
    }
}

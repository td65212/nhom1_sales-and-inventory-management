using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

#nullable disable

namespace nhom1_salesandinventorymanagement.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260616190000_AddProductPricesAndStockReceiptApproval")]
public class AddProductPricesAndStockReceiptApproval : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "OriginalPrice",
            table: "Products",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "SalePrice",
            table: "Products",
            type: "numeric(18,2)",
            nullable: true);

        migrationBuilder.Sql("UPDATE \"Products\" SET \"OriginalPrice\" = \"SellingPrice\" WHERE \"OriginalPrice\" = 0;");

        migrationBuilder.AddColumn<string>(
            name: "InvoiceNumber",
            table: "StockReceipts",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTime>(
            name: "ImportDate",
            table: "StockReceipts",
            type: "date",
            nullable: false,
            defaultValueSql: "CURRENT_DATE");

        migrationBuilder.AddColumn<DateTime>(
            name: "SubmittedAt",
            table: "StockReceipts",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ApprovedAt",
            table: "StockReceipts",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ApprovedByUserId",
            table: "StockReceipts",
            type: "integer",
            nullable: true);

        migrationBuilder.Sql("UPDATE \"StockReceipts\" SET \"Status\" = 3 WHERE \"Status\" = 2;");
        migrationBuilder.Sql("UPDATE \"StockReceipts\" SET \"Status\" = 2, \"ApprovedAt\" = \"ConfirmedAt\" WHERE \"Status\" = 1;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE \"StockReceipts\" SET \"Status\" = 1 WHERE \"Status\" = 2;");
        migrationBuilder.Sql("UPDATE \"StockReceipts\" SET \"Status\" = 2 WHERE \"Status\" = 3;");

        migrationBuilder.DropColumn(name: "ApprovedAt", table: "StockReceipts");
        migrationBuilder.DropColumn(name: "ApprovedByUserId", table: "StockReceipts");
        migrationBuilder.DropColumn(name: "ImportDate", table: "StockReceipts");
        migrationBuilder.DropColumn(name: "InvoiceNumber", table: "StockReceipts");
        migrationBuilder.DropColumn(name: "SubmittedAt", table: "StockReceipts");
        migrationBuilder.DropColumn(name: "OriginalPrice", table: "Products");
        migrationBuilder.DropColumn(name: "SalePrice", table: "Products");
    }
}

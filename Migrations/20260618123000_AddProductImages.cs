using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

#nullable disable

namespace nhom1_salesandinventorymanagement.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260618123000_AddProductImages")]
public class AddProductImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProductImages",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ProductId = table.Column<int>(type: "integer", nullable: false),
                ImageUrl = table.Column<string>(type: "text", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductImages", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductImages_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProductImages_ProductId_SortOrder",
            table: "ProductImages",
            columns: new[] { "ProductId", "SortOrder" });

        migrationBuilder.Sql("""
            INSERT INTO "ProductImages" ("ProductId", "ImageUrl", "SortOrder")
            SELECT "Id", "ImageUrl", 0
            FROM "Products"
            WHERE "ImageUrl" IS NOT NULL
              AND length(trim("ImageUrl")) > 0
              AND NOT EXISTS (
                  SELECT 1
                  FROM "ProductImages"
                  WHERE "ProductImages"."ProductId" = "Products"."Id"
                    AND "ProductImages"."ImageUrl" = "Products"."ImageUrl"
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProductImages");
    }
}

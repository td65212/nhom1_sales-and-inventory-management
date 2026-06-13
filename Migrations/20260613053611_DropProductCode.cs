using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nhom1_salesandinventorymanagement.Migrations
{
    /// <inheritdoc />
    public partial class DropProductCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

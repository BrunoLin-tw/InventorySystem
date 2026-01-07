using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventorySystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxIdFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "Suppliers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Suppliers",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "Customers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Customers",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Customers");
        }
    }
}

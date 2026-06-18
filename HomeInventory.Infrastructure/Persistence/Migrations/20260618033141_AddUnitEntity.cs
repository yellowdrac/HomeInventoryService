using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HomeInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Items");

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_units", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "units",
                columns: new[] { "Id", "Category", "Name", "SortOrder", "Symbol" },
                values: new object[,]
                {
                    { new Guid("10000001-0000-0000-0000-000000000001"), "Count", "Unit", 1, "unit" },
                    { new Guid("10000001-0000-0000-0000-000000000002"), "Count", "Pack", 2, "pack" },
                    { new Guid("10000001-0000-0000-0000-000000000003"), "Count", "Box", 3, "box" },
                    { new Guid("10000001-0000-0000-0000-000000000004"), "Count", "Bag", 4, "bag" },
                    { new Guid("10000001-0000-0000-0000-000000000005"), "Count", "Bottle", 5, "bottle" },
                    { new Guid("10000001-0000-0000-0000-000000000006"), "Count", "Can", 6, "can" },
                    { new Guid("10000001-0000-0000-0000-000000000007"), "Count", "Jar", 7, "jar" },
                    { new Guid("10000001-0000-0000-0000-000000000008"), "Count", "Roll", 8, "roll" },
                    { new Guid("10000001-0000-0000-0000-000000000009"), "Weight", "Gram", 10, "g" },
                    { new Guid("10000001-0000-0000-0000-000000000010"), "Weight", "Kilogram", 11, "kg" },
                    { new Guid("10000001-0000-0000-0000-000000000011"), "Weight", "Milligram", 12, "mg" },
                    { new Guid("10000001-0000-0000-0000-000000000012"), "Weight", "Pound", 13, "lb" },
                    { new Guid("10000001-0000-0000-0000-000000000014"), "Volume", "Milliliter", 20, "mL" },
                    { new Guid("10000001-0000-0000-0000-000000000015"), "Volume", "Liter", 21, "L" },
                    { new Guid("10000001-0000-0000-0000-000000000016"), "Volume", "Cup", 22, "cup" },
                    { new Guid("10000001-0000-0000-0000-000000000017"), "Volume", "Tablespoon", 23, "tbsp" },
                    { new Guid("10000001-0000-0000-0000-000000000018"), "Volume", "Teaspoon", 24, "tsp" },
                    { new Guid("10000001-0000-0000-0000-000000000019"), "Volume", "Fluid Ounce", 25, "fl oz" },
                    { new Guid("10000001-0000-0000-0000-000000000020"), "Length", "Meter", 30, "m" },
                    { new Guid("10000001-0000-0000-0000-000000000021"), "Length", "Centimeter", 31, "cm" },
                    { new Guid("10000001-0000-0000-0000-000000000022"), "Length", "Millimeter", 32, "mm" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_UnitId",
                table: "Items",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_units_UnitId",
                table: "Items",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_units_UnitId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "units");

            migrationBuilder.DropIndex(
                name: "IX_Items_UnitId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Items");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }
    }
}

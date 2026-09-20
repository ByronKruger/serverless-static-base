using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Coffeeg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BeveragePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BeverageTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeverageTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    MinValue = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BeveragePreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BeverageTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeveragePreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeveragePreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeveragePreferences_BeverageTypes_BeverageTypeId",
                        column: x => x.BeverageTypeId,
                        principalTable: "BeverageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ParentIngredientId = table.Column<int>(type: "int", nullable: true),
                    UnitTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ingredients_Ingredients_ParentIngredientId",
                        column: x => x.ParentIngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ingredients_UnitTypes_UnitTypeId",
                        column: x => x.UnitTypeId,
                        principalTable: "UnitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BeveragePreferenceIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeveragePreferenceId = table.Column<int>(type: "int", nullable: false),
                    IngredientId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeveragePreferenceIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeveragePreferenceIngredients_BeveragePreferences_BeveragePreferenceId",
                        column: x => x.BeveragePreferenceId,
                        principalTable: "BeveragePreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeveragePreferenceIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BeverageTypeIngredients",
                columns: table => new
                {
                    BeverageTypeId = table.Column<int>(type: "int", nullable: false),
                    IngredientId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeverageTypeIngredients", x => new { x.BeverageTypeId, x.IngredientId });
                    table.ForeignKey(
                        name: "FK_BeverageTypeIngredients_BeverageTypes_BeverageTypeId",
                        column: x => x.BeverageTypeId,
                        principalTable: "BeverageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeverageTypeIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "BeverageTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Brewed coffee, however you take it.", "Coffee" },
                    { 2, "Neat, on the rocks, or with a splash of water.", "Whiskey" }
                });

            migrationBuilder.InsertData(
                table: "UnitTypes",
                columns: new[] { "Id", "Kind", "MaxValue", "MinValue", "Name", "Symbol" },
                values: new object[,]
                {
                    { 1, "Numeric", 20m, 0m, "Teaspoons", "tsp" },
                    { 2, "Numeric", 100m, 0m, "Percentage", "%" },
                    { 3, "Boolean", 1m, 1m, "Yes / no", null },
                    { 4, "Numeric", 10m, 0m, "Shots", "shot" },
                    { 5, "Numeric", 1000m, 0m, "Millilitres", "ml" }
                });

            migrationBuilder.InsertData(
                table: "Ingredients",
                columns: new[] { "Id", "Description", "Name", "ParentIngredientId", "UnitTypeId" },
                values: new object[,]
                {
                    { 1, "Ground coffee.", "Coffee", null, 1 },
                    { 2, "Pick which milk.", "Milk", null, 2 },
                    { 6, "Pick which sweetener.", "Sugar", null, 1 },
                    { 9, null, "Water", null, 5 },
                    { 10, null, "Whiskey", null, 4 },
                    { 11, "With or without.", "Ice", null, 3 }
                });

            migrationBuilder.InsertData(
                table: "BeverageTypeIngredients",
                columns: new[] { "BeverageTypeId", "IngredientId", "SortOrder" },
                values: new object[,]
                {
                    { 1, 1, 0 },
                    { 1, 2, 1 },
                    { 1, 6, 2 },
                    { 1, 9, 3 },
                    { 2, 9, 2 },
                    { 2, 10, 0 },
                    { 2, 11, 1 }
                });

            migrationBuilder.InsertData(
                table: "Ingredients",
                columns: new[] { "Id", "Description", "Name", "ParentIngredientId", "UnitTypeId" },
                values: new object[,]
                {
                    { 3, null, "Low-fat milk", 2, 2 },
                    { 4, null, "Raw milk", 2, 2 },
                    { 5, null, "Soy milk", 2, 2 },
                    { 7, null, "White sugar", 6, 1 },
                    { 8, null, "Honey", 6, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeveragePreferenceIngredients_BeveragePreferenceId_IngredientId",
                table: "BeveragePreferenceIngredients",
                columns: new[] { "BeveragePreferenceId", "IngredientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeveragePreferenceIngredients_IngredientId",
                table: "BeveragePreferenceIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_BeveragePreferences_BeverageTypeId",
                table: "BeveragePreferences",
                column: "BeverageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BeveragePreferences_UserId_BeverageTypeId",
                table: "BeveragePreferences",
                columns: new[] { "UserId", "BeverageTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeverageTypeIngredients_IngredientId",
                table: "BeverageTypeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_BeverageTypes_Name",
                table: "BeverageTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_ParentIngredientId_Name",
                table: "Ingredients",
                columns: new[] { "ParentIngredientId", "Name" },
                unique: true,
                filter: "[ParentIngredientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_UnitTypeId",
                table: "Ingredients",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitTypes_Name",
                table: "UnitTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeveragePreferenceIngredients");

            migrationBuilder.DropTable(
                name: "BeverageTypeIngredients");

            migrationBuilder.DropTable(
                name: "BeveragePreferences");

            migrationBuilder.DropTable(
                name: "Ingredients");

            migrationBuilder.DropTable(
                name: "BeverageTypes");

            migrationBuilder.DropTable(
                name: "UnitTypes");
        }
    }
}

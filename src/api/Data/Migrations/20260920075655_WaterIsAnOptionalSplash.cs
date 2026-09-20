using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coffeeg.Api.Data.Migrations
{
    /// <summary>
    /// Water becomes an optional splash rather than a volume, and coffee stops naming it.
    ///
    /// The two scaffolded operations move the seed. The two hand-written statements after
    /// them repair preferences people had already saved, which the scaffold cannot know
    /// about: a coffee with "180 ml of water" now references an ingredient its beverage no
    /// longer composes, and 180 is not a legal amount for a Boolean unit.
    ///
    /// Both statements are written against the invariant rather than against water, so
    /// they stay correct if the composition is edited again.
    /// </summary>
    public partial class WaterIsAnOptionalSplash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BeverageTypeIngredients",
                keyColumns: new[] { "BeverageTypeId", "IngredientId" },
                keyValues: new object[] { 1, 9 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Description", "UnitTypeId" },
                values: new object[] { "With or without a splash.", 3 });

            // Drop every saved line whose ingredient is no longer part of its beverage,
            // directly or as a variant of one that is. Runs after the DeleteData above, so
            // it sees the new composition.
            migrationBuilder.Sql(@"
                DELETE line
                FROM BeveragePreferenceIngredients AS line
                JOIN BeveragePreferences AS preference
                  ON preference.Id = line.BeveragePreferenceId
                JOIN Ingredients AS ingredient
                  ON ingredient.Id = line.IngredientId
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM BeverageTypeIngredients AS composition
                    WHERE composition.BeverageTypeId = preference.BeverageTypeId
                      AND composition.IngredientId IN (
                          ingredient.Id,
                          ISNULL(ingredient.ParentIngredientId, ingredient.Id)));");

            // A Boolean unit says only yes (1) or no (null). Any surviving quantity is
            // read as intent: nothing of it becomes no, anything else becomes yes.
            migrationBuilder.Sql(@"
                UPDATE line
                SET line.Amount = CASE WHEN line.Amount = 0 THEN NULL ELSE 1 END
                FROM BeveragePreferenceIngredients AS line
                JOIN Ingredients AS ingredient
                  ON ingredient.Id = line.IngredientId
                JOIN UnitTypes AS unit
                  ON unit.Id = ingredient.UnitTypeId
                WHERE unit.Kind = 'Boolean'
                  AND line.Amount IS NOT NULL
                  AND line.Amount <> 1;");
        }

        /// <inheritdoc />
        /// <remarks>
        /// Restores the catalogue, not the data. The volumes people had saved against
        /// water were collapsed to yes/no by <c>Up</c> and cannot be recovered, and the
        /// coffee water lines it deleted are gone. Rolling back leaves coffee offering
        /// water again with nobody having an amount for it.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Description", "UnitTypeId" },
                values: new object[] { null, 5 });

            migrationBuilder.InsertData(
                table: "BeverageTypeIngredients",
                columns: new[] { "BeverageTypeId", "IngredientId", "SortOrder" },
                values: new object[] { 1, 9, 3 });
        }
    }
}

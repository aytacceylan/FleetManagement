using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDeparturesConstraintNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PK_Makams -> PK_Departures
            migrationBuilder.DropPrimaryKey(
                name: "PK_Makams",
                table: "Departures");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Departures",
                table: "Departures",
                column: "Id");

            // IX_Makams_Code -> IX_Departures_Code
            migrationBuilder.DropIndex(
                name: "IX_Makams_Code",
                table: "Departures");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_Code",
                table: "Departures",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Departures",
                table: "Departures");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Makams",
                table: "Departures",
                column: "Id");

            migrationBuilder.DropIndex(
                name: "IX_Departures_Code",
                table: "Departures");

            migrationBuilder.CreateIndex(
                name: "IX_Makams_Code",
                table: "Departures",
                column: "Code",
                unique: true);
        }
    }
}

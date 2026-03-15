using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixVehicleIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_InventoryNumber",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_Plate",
                table: "Vehicles");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_InventoryNumber",
                table: "Vehicles",
                column: "InventoryNumber",
                unique: true,
                filter: "\"IsDeleted\" = false AND \"InventoryNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Plate",
                table: "Vehicles",
                column: "Plate",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_InventoryNumber",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_Plate",
                table: "Vehicles");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_InventoryNumber",
                table: "Vehicles",
                column: "InventoryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Plate",
                table: "Vehicles",
                column: "Plate",
                unique: true);
        }
    }
}

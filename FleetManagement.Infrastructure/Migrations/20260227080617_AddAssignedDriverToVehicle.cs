using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedDriverToVehicle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedDriverId",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_AssignedDriverId",
                table: "Vehicles",
                column: "AssignedDriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Drivers_AssignedDriverId",
                table: "Vehicles",
                column: "AssignedDriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Drivers_AssignedDriverId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_AssignedDriverId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AssignedDriverId",
                table: "Vehicles");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecondDriverToVehicleMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecondDriverId",
                table: "VehicleMovements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondDriverText",
                table: "VehicleMovements",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMovements_SecondDriverId",
                table: "VehicleMovements",
                column: "SecondDriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleMovements_Drivers_SecondDriverId",
                table: "VehicleMovements",
                column: "SecondDriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleMovements_Drivers_SecondDriverId",
                table: "VehicleMovements");

            migrationBuilder.DropIndex(
                name: "IX_VehicleMovements_SecondDriverId",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "SecondDriverId",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "SecondDriverText",
                table: "VehicleMovements");
        }
    }
}

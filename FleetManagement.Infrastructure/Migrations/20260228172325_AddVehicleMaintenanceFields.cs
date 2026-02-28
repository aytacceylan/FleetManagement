using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleMaintenanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastMaintenanceDate",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastMaintenanceKm",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaintenanceIntervalKm",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaintenanceIntervalMonths",
                table: "Vehicles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMaintenanceDate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LastMaintenanceKm",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MaintenanceIntervalKm",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MaintenanceIntervalMonths",
                table: "Vehicles");
        }
    }
}

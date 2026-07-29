using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MissionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualExitDateTime",
                table: "VehicleMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualReturnDateTime",
                table: "VehicleMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelDateTime",
                table: "VehicleMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "VehicleMovements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedExitDateTime",
                table: "VehicleMovements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "VehicleMovements",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualExitDateTime",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "ActualReturnDateTime",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "CancelDateTime",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "PlannedExitDateTime",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "VehicleMovements");
        }
    }
}

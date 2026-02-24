using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeVehicleMovementOptionalAndFreeText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "VehicleId",
                table: "VehicleMovements",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "VehicleCommanderId",
                table: "VehicleMovements",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "StartKm",
                table: "VehicleMovements",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "DriverId",
                table: "VehicleMovements",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "CommanderText",
                table: "VehicleMovements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverText",
                table: "VehicleMovements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiclePlateText",
                table: "VehicleMovements",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommanderText",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "DriverText",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "VehiclePlateText",
                table: "VehicleMovements");

            migrationBuilder.AlterColumn<int>(
                name: "VehicleId",
                table: "VehicleMovements",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "VehicleCommanderId",
                table: "VehicleMovements",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StartKm",
                table: "VehicleMovements",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DriverId",
                table: "VehicleMovements",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}

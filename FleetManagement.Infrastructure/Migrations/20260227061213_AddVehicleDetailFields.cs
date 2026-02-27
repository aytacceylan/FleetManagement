using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Plate",
                table: "Vehicles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Vehicles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Vehicles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "InventoryNumber",
                table: "Vehicles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoadCapacity",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotorNo",
                table: "Vehicles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PassengerCapacity",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SaseNo",
                table: "Vehicles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleCategory",
                table: "Vehicles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleKm",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleSituation",
                table: "Vehicles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "Vehicles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleUnit",
                table: "Vehicles",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Route",
                table: "VehicleMovements",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Purpose",
                table: "VehicleMovements",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_InventoryNumber",
                table: "Vehicles",
                column: "InventoryNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_InventoryNumber",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "InventoryNumber",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LoadCapacity",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MotorNo",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "PassengerCapacity",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "SaseNo",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleCategory",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleKm",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleSituation",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleUnit",
                table: "Vehicles");

            migrationBuilder.AlterColumn<string>(
                name: "Plate",
                table: "Vehicles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Vehicles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Vehicles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Route",
                table: "VehicleMovements",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Purpose",
                table: "VehicleMovements",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}

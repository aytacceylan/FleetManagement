using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeVehicleGuardNumberNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eski unique index'i kaldır (varsa)
            migrationBuilder.DropIndex(
                name: "IX_VehicleGuards_GuardNumber",
                table: "VehicleGuards");

            // GuardNumber nullable
            migrationBuilder.AlterColumn<string>(
                name: "GuardNumber",
                table: "VehicleGuards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            // Null serbest, dolu ise unique
            migrationBuilder.CreateIndex(
                name: "IX_VehicleGuards_GuardNumber",
                table: "VehicleGuards",
                column: "GuardNumber",
                unique: true,
                filter: "\"GuardNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleGuards_GuardNumber",
                table: "VehicleGuards");

            migrationBuilder.AlterColumn<string>(
                name: "GuardNumber",
                table: "VehicleGuards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGuards_GuardNumber",
                table: "VehicleGuards",
                column: "GuardNumber",
                unique: true);
        }
    }
}

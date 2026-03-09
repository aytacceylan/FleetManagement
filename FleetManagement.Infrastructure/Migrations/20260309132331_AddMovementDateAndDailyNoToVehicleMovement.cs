using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMovementDateAndDailyNoToVehicleMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyNo",
                table: "VehicleMovements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "MovementDate",
                table: "VehicleMovements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(@"
        UPDATE ""VehicleMovements""
        SET ""MovementDate"" = DATE(""ExitDateTime"");

        WITH ordered AS
        (
            SELECT
                ""Id"",
                ROW_NUMBER() OVER (
                    PARTITION BY ""MovementDate""
                    ORDER BY ""ExitDateTime"", ""Id""
                ) AS new_no
            FROM ""VehicleMovements""
            WHERE ""IsDeleted"" = false
        )
        UPDATE ""VehicleMovements"" vm
        SET ""DailyNo"" = ordered.new_no
        FROM ordered
        WHERE vm.""Id"" = ordered.""Id"";
    ");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMovements_MovementDate_DailyNo",
                table: "VehicleMovements",
                columns: new[] { "MovementDate", "DailyNo" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleMovements_MovementDate_DailyNo",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "DailyNo",
                table: "VehicleMovements");

            migrationBuilder.DropColumn(
                name: "MovementDate",
                table: "VehicleMovements");
        }
    }
}

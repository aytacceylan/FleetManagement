using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSoftDeleteUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleYears_Year",
                table: "VehicleYears");

            migrationBuilder.DropIndex(
                name: "IX_VehicleModels_Code",
                table: "VehicleModels");

            migrationBuilder.DropIndex(
                name: "IX_VehicleCommanders_CommanderNumber",
                table: "VehicleCommanders");

            migrationBuilder.DropIndex(
                name: "IX_VehicleCategories_Code",
                table: "VehicleCategories");

            migrationBuilder.DropIndex(
                name: "IX_VehicleBrands_Code",
                table: "VehicleBrands");

            migrationBuilder.DropIndex(
                name: "IX_Routes_Code",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_DriverNumber",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Departures_Code",
                table: "Departures");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleYears_Year",
                table: "VehicleYears",
                column: "Year",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_Code",
                table: "VehicleModels",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCommanders_CommanderNumber",
                table: "VehicleCommanders",
                column: "CommanderNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCategories_Code",
                table: "VehicleCategories",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBrands_Code",
                table: "VehicleBrands",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Code",
                table: "Routes",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_DriverNumber",
                table: "Drivers",
                column: "DriverNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_Code",
                table: "Departures",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleYears_Year",
                table: "VehicleYears");

            migrationBuilder.DropIndex(
                name: "IX_VehicleModels_Code",
                table: "VehicleModels");

            migrationBuilder.DropIndex(
                name: "IX_VehicleCommanders_CommanderNumber",
                table: "VehicleCommanders");

            migrationBuilder.DropIndex(
                name: "IX_VehicleCategories_Code",
                table: "VehicleCategories");

            migrationBuilder.DropIndex(
                name: "IX_VehicleBrands_Code",
                table: "VehicleBrands");

            migrationBuilder.DropIndex(
                name: "IX_Routes_Code",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_DriverNumber",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Departures_Code",
                table: "Departures");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleYears_Year",
                table: "VehicleYears",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_Code",
                table: "VehicleModels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCommanders_CommanderNumber",
                table: "VehicleCommanders",
                column: "CommanderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCategories_Code",
                table: "VehicleCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBrands_Code",
                table: "VehicleBrands",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Code",
                table: "Routes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_DriverNumber",
                table: "Drivers",
                column: "DriverNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departures_Code",
                table: "Departures",
                column: "Code",
                unique: true);
        }
    }
}

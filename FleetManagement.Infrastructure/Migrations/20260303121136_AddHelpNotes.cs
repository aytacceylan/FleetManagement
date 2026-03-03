using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FleetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HelpNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpNotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DutyTypes_Code",
                table: "DutyTypes",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false AND \"Code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DutyTypes_Name",
                table: "DutyTypes",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_HelpNotes_Title",
                table: "HelpNotes",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HelpNotes");

            migrationBuilder.DropIndex(
                name: "IX_DutyTypes_Code",
                table: "DutyTypes");

            migrationBuilder.DropIndex(
                name: "IX_DutyTypes_Name",
                table: "DutyTypes");
        }
    }
}

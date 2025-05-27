using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUser_DeletedTs_DisabledTs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedTs",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisabledTs",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedTs",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DisabledTs",
                table: "Users");
        }
    }
}

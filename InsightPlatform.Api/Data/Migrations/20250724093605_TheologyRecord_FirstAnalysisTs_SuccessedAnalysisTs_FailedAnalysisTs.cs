using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TheologyRecord_FirstAnalysisTs_SuccessedAnalysisTs_FailedAnalysisTs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAnalysisTs",
                table: "TheologyRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstAnalysisTs",
                table: "TheologyRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuccessedAnalysisTs",
                table: "TheologyRecords",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedAnalysisTs",
                table: "TheologyRecords");

            migrationBuilder.DropColumn(
                name: "FirstAnalysisTs",
                table: "TheologyRecords");

            migrationBuilder.DropColumn(
                name: "SuccessedAnalysisTs",
                table: "TheologyRecords");
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TheologyRecord_CombinedPrompts_LastAnalysisTs_FailedCount_Errors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CombinedPrompts",
                table: "TheologyRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Errors",
                table: "TheologyRecords",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedCount",
                table: "TheologyRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAnalysisTs",
                table: "TheologyRecords",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CombinedPrompts",
                table: "TheologyRecords");

            migrationBuilder.DropColumn(
                name: "Errors",
                table: "TheologyRecords");

            migrationBuilder.DropColumn(
                name: "FailedCount",
                table: "TheologyRecords");

            migrationBuilder.DropColumn(
                name: "LastAnalysisTs",
                table: "TheologyRecords");
        }
    }
}

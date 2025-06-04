using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Update_LuckyNumberRecordByKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Result",
                table: "LuckyNumberRecordByKinds");

            migrationBuilder.RenameColumn(
                name: "DisplayOrderInDate",
                table: "LuckyNumberRecordByKinds",
                newName: "DisplayOrder");

            migrationBuilder.AddColumn<List<string>>(
                name: "Numbers",
                table: "LuckyNumberRecordByKinds",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Numbers",
                table: "LuckyNumberRecordByKinds");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "LuckyNumberRecordByKinds",
                newName: "DisplayOrderInDate");

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "LuckyNumberRecordByKinds",
                type: "text",
                nullable: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Update_LuckyNumberRecordByKind_DisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "LuckyNumberRecordByKinds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "LuckyNumberRecordByKinds",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

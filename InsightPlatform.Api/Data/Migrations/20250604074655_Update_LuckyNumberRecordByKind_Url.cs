using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Update_LuckyNumberRecordByKind_Url : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "LuckyNumberRecordByKinds",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "LuckyNumberRecordByKinds");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TheologyRecord_Status_PreData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreData",
                table: "TheologyRecords",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "TheologyRecords",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreData",
                table: "TheologyRecords");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TheologyRecords");
        }
    }
}

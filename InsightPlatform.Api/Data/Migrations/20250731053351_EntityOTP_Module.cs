using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EntityOTP_Module : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "Module",
                table: "EntityOTPs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Module",
                table: "EntityOTPs");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AffiliateProducts_Slug",
                table: "AffiliateProducts");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "AffiliateProducts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "AffiliateProducts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateProducts_Slug",
                table: "AffiliateProducts",
                column: "Slug",
                unique: true);
        }
    }
}

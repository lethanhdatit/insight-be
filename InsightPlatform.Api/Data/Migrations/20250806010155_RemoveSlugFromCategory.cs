using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSlugFromCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AffiliateCategories_Slug",
                table: "AffiliateCategories");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "AffiliateCategories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "AffiliateCategories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateCategories_Slug",
                table: "AffiliateCategories",
                column: "Slug",
                unique: true);
        }
    }
}

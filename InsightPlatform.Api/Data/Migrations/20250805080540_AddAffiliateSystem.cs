using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAffiliateSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AffiliateCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Slug = table.Column<string>(type: "text", nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LocalizedContent = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateCategories_AffiliateCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "AffiliateCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AffiliateProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderId = table.Column<string>(type: "text", nullable: true),
                    Provider = table.Column<byte>(type: "smallint", nullable: false),
                    ProviderUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalSold = table.Column<int>(type: "integer", nullable: false),
                    SaleLocation = table.Column<string>(type: "text", nullable: true),
                    LocalizedContent = table.Column<string>(type: "jsonb", nullable: true),
                    Images = table.Column<string>(type: "jsonb", nullable: true),
                    Attributes = table.Column<string>(type: "jsonb", nullable: true),
                    Labels = table.Column<string>(type: "jsonb", nullable: true),
                    Variants = table.Column<string>(type: "jsonb", nullable: true),
                    SellerInfo = table.Column<string>(type: "jsonb", nullable: true),
                    LastSyncedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AffiliateCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateProducts_AffiliateCategories_AffiliateCategoryId",
                        column: x => x.AffiliateCategoryId,
                        principalTable: "AffiliateCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AffiliateFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateFavorites_AffiliateProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AffiliateProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AffiliateFavorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AffiliateProductCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateProductCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateProductCategories_AffiliateCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AffiliateCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AffiliateProductCategories_AffiliateProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AffiliateProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AffiliateTrackingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<byte>(type: "smallint", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    ClientIP = table.Column<string>(type: "text", nullable: true),
                    Referrer = table.Column<string>(type: "text", nullable: true),
                    MetaData = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateTrackingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateTrackingEvents_AffiliateCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AffiliateCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AffiliateTrackingEvents_AffiliateProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AffiliateProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AffiliateTrackingEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateCategories_Code",
                table: "AffiliateCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateCategories_ParentId",
                table: "AffiliateCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateCategories_Slug",
                table: "AffiliateCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateFavorites_ProductId",
                table: "AffiliateFavorites",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateFavorites_UserId_ProductId",
                table: "AffiliateFavorites",
                columns: new[] { "UserId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateProductCategories_CategoryId",
                table: "AffiliateProductCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateProductCategories_ProductId_CategoryId",
                table: "AffiliateProductCategories",
                columns: new[] { "ProductId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateProducts_AffiliateCategoryId",
                table: "AffiliateProducts",
                column: "AffiliateCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateProducts_Provider_ProviderId",
                table: "AffiliateProducts",
                columns: new[] { "Provider", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateProducts_Slug",
                table: "AffiliateProducts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateTrackingEvents_CategoryId",
                table: "AffiliateTrackingEvents",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateTrackingEvents_CreatedTs",
                table: "AffiliateTrackingEvents",
                column: "CreatedTs");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateTrackingEvents_ProductId_CreatedTs",
                table: "AffiliateTrackingEvents",
                columns: new[] { "ProductId", "CreatedTs" });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateTrackingEvents_UserId_CreatedTs",
                table: "AffiliateTrackingEvents",
                columns: new[] { "UserId", "CreatedTs" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffiliateFavorites");

            migrationBuilder.DropTable(
                name: "AffiliateProductCategories");

            migrationBuilder.DropTable(
                name: "AffiliateTrackingEvents");

            migrationBuilder.DropTable(
                name: "AffiliateProducts");

            migrationBuilder.DropTable(
                name: "AffiliateCategories");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LuckyNumberProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    HomePage = table.Column<string>(type: "text", nullable: true),
                    UrlPathTemplate = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LuckyNumberProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LuckyNumberRecordByKinds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DisplayOrderInDate = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LuckyNumberRecordByKinds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Username = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    PasswordSalt = table.Column<string>(type: "text", nullable: true),
                    GoogleId = table.Column<string>(type: "text", nullable: true),
                    FacebookId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    ClientLocale = table.Column<string>(type: "text", nullable: true),
                    DeletedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisabledTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LuckyNumberRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Detail = table.Column<string>(type: "jsonb", nullable: true),
                    CrawlUrl = table.Column<string>(type: "text", nullable: true),
                    CrossCheckedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CrossCheckedProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LuckyNumberRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LuckyNumberRecords_LuckyNumberProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "LuckyNumberProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Pains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PainDetail = table.Column<string>(type: "text", nullable: true),
                    Desire = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Topic = table.Column<string>(type: "text", nullable: true),
                    Emotion = table.Column<string>(type: "text", nullable: true),
                    UrgencyLevel = table.Column<int>(type: "integer", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    ClientLocale = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pains_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LuckyNumberRecords_ProviderId",
                table: "LuckyNumberRecords",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Pains_UserId",
                table: "Pains",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LuckyNumberRecordByKinds");

            migrationBuilder.DropTable(
                name: "LuckyNumberRecords");

            migrationBuilder.DropTable(
                name: "Pains");

            migrationBuilder.DropTable(
                name: "LuckyNumberProviders");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

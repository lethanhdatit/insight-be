using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntityOTPs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Module = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: true),
                    OTP = table.Column<string>(type: "text", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityOTPs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LuckyNumberProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    HomePage = table.Column<string>(type: "text", nullable: true),
                    UrlPathTemplate = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Url = table.Column<string>(type: "text", nullable: true),
                    Numbers = table.Column<List<string>>(type: "text[]", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LuckyNumberRecordByKinds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ServiceKind = table.Column<byte>(type: "smallint", nullable: false),
                    Fates = table.Column<int>(type: "integer", nullable: false),
                    FatesDiscount = table.Column<int>(type: "integer", nullable: true),
                    FatesDiscountRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopUpPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    AmountDiscount = table.Column<decimal>(type: "numeric", nullable: true),
                    AmountDiscountRate = table.Column<decimal>(type: "numeric", nullable: true),
                    Fates = table.Column<int>(type: "integer", nullable: false),
                    FateBonus = table.Column<int>(type: "integer", nullable: true),
                    FateBonusRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopUpPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Username = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    PasswordSalt = table.Column<string>(type: "text", nullable: true),
                    GoogleId = table.Column<string>(type: "text", nullable: true),
                    FacebookId = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    ClientLocale = table.Column<string>(type: "text", nullable: true),
                    Fates = table.Column<int>(type: "integer", nullable: false),
                    DeletedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisabledTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "TheologyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    ServicePriceSnap = table.Column<string>(type: "jsonb", nullable: true),
                    Input = table.Column<string>(type: "jsonb", nullable: true),
                    PreData = table.Column<string>(type: "jsonb", nullable: true),
                    SystemPrompt = table.Column<string>(type: "text", nullable: true),
                    UserPrompt = table.Column<string>(type: "text", nullable: true),
                    CombinedPrompts = table.Column<string>(type: "text", nullable: true),
                    UniqueKey = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<string>(type: "jsonb", nullable: true),
                    FirstAnalysisTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAnalysisTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuccessedAnalysisTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAnalysisTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedCount = table.Column<int>(type: "integer", nullable: true),
                    Errors = table.Column<List<string>>(type: "text[]", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheologyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TheologyRecords_ServicePrices_ServicePriceId",
                        column: x => x.ServicePriceId,
                        principalTable: "ServicePrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TheologyRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopUpPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    FinalTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    FeeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    FeeTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    BuyerPaysFee = table.Column<bool>(type: "boolean", nullable: false),
                    VATaxRate = table.Column<decimal>(type: "numeric", nullable: false),
                    VATaxTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    VATaxIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: true),
                    Provider = table.Column<byte>(type: "smallint", nullable: false),
                    ProviderTransaction = table.Column<string>(type: "jsonb", nullable: true),
                    MetaData = table.Column<string>(type: "jsonb", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_TopUpPackages_TopUpPackageId",
                        column: x => x.TopUpPackageId,
                        principalTable: "TopUpPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FatePointTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TheologyRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Fates = table.Column<int>(type: "integer", nullable: false),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FatePointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FatePointTransactions_TheologyRecords_TheologyRecordId",
                        column: x => x.TheologyRecordId,
                        principalTable: "TheologyRecords",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FatePointTransactions_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FatePointTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FatePointTransactions_TheologyRecordId",
                table: "FatePointTransactions",
                column: "TheologyRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_FatePointTransactions_TransactionId",
                table: "FatePointTransactions",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FatePointTransactions_UserId",
                table: "FatePointTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LuckyNumberRecords_ProviderId",
                table: "LuckyNumberRecords",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Pains_UserId",
                table: "Pains",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TheologyRecords_ServicePriceId",
                table: "TheologyRecords",
                column: "ServicePriceId");

            migrationBuilder.CreateIndex(
                name: "IX_TheologyRecords_UserId",
                table: "TheologyRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TopUpPackageId",
                table: "Transactions",
                column: "TopUpPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityOTPs");

            migrationBuilder.DropTable(
                name: "FatePointTransactions");

            migrationBuilder.DropTable(
                name: "LuckyNumberRecordByKinds");

            migrationBuilder.DropTable(
                name: "LuckyNumberRecords");

            migrationBuilder.DropTable(
                name: "Pains");

            migrationBuilder.DropTable(
                name: "TheologyRecords");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "LuckyNumberProviders");

            migrationBuilder.DropTable(
                name: "ServicePrices");

            migrationBuilder.DropTable(
                name: "TopUpPackages");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

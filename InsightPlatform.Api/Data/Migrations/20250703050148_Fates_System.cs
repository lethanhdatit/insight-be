using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fates_System : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Fates",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ServicePrice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ServiceKind = table.Column<byte>(type: "smallint", nullable: false),
                    Fates = table.Column<int>(type: "integer", nullable: false),
                    FatesDiscount = table.Column<int>(type: "integer", nullable: true),
                    FatesDiscountRate = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopUpPackage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    AmountDiscount = table.Column<decimal>(type: "numeric", nullable: true),
                    AmountDiscountRate = table.Column<double>(type: "double precision", nullable: true),
                    Fate = table.Column<int>(type: "integer", nullable: false),
                    FateBonus = table.Column<int>(type: "integer", nullable: true),
                    FateBonusRate = table.Column<double>(type: "double precision", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopUpPackage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopUpPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Provider = table.Column<byte>(type: "smallint", nullable: false),
                    ProviderTransaction = table.Column<string>(type: "jsonb", nullable: true),
                    MetaData = table.Column<string>(type: "jsonb", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transaction_TopUpPackage_TopUpPackageId",
                        column: x => x.TopUpPackageId,
                        principalTable: "TopUpPackage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transaction_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FatePointTransaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePriceId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Fates = table.Column<int>(type: "integer", nullable: false),
                    CreatedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FatePointTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FatePointTransaction_ServicePrice_ServicePriceId",
                        column: x => x.ServicePriceId,
                        principalTable: "ServicePrice",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FatePointTransaction_Transaction_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transaction",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FatePointTransaction_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FatePointTransaction_ServicePriceId",
                table: "FatePointTransaction",
                column: "ServicePriceId");

            migrationBuilder.CreateIndex(
                name: "IX_FatePointTransaction_TransactionId",
                table: "FatePointTransaction",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FatePointTransaction_UserId",
                table: "FatePointTransaction",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_TopUpPackageId",
                table: "Transaction",
                column: "TopUpPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_UserId",
                table: "Transaction",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FatePointTransaction");

            migrationBuilder.DropTable(
                name: "ServicePrice");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "TopUpPackage");

            migrationBuilder.DropColumn(
                name: "Fates",
                table: "Users");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fates_System_Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FatePointTransaction_ServicePrice_ServicePriceId",
                table: "FatePointTransaction");

            migrationBuilder.RenameColumn(
                name: "ServicePriceId",
                table: "FatePointTransaction",
                newName: "TheologyRecordId");

            migrationBuilder.RenameIndex(
                name: "IX_FatePointTransaction_ServicePriceId",
                table: "FatePointTransaction",
                newName: "IX_FatePointTransaction_TheologyRecordId");

            migrationBuilder.AddColumn<Guid>(
                name: "ServicePriceId",
                table: "TheologyRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TheologyRecords_ServicePriceId",
                table: "TheologyRecords",
                column: "ServicePriceId");

            migrationBuilder.AddForeignKey(
                name: "FK_FatePointTransaction_TheologyRecords_TheologyRecordId",
                table: "FatePointTransaction",
                column: "TheologyRecordId",
                principalTable: "TheologyRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TheologyRecords_ServicePrice_ServicePriceId",
                table: "TheologyRecords",
                column: "ServicePriceId",
                principalTable: "ServicePrice",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FatePointTransaction_TheologyRecords_TheologyRecordId",
                table: "FatePointTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_TheologyRecords_ServicePrice_ServicePriceId",
                table: "TheologyRecords");

            migrationBuilder.DropIndex(
                name: "IX_TheologyRecords_ServicePriceId",
                table: "TheologyRecords");

            migrationBuilder.DropColumn(
                name: "ServicePriceId",
                table: "TheologyRecords");

            migrationBuilder.RenameColumn(
                name: "TheologyRecordId",
                table: "FatePointTransaction",
                newName: "ServicePriceId");

            migrationBuilder.RenameIndex(
                name: "IX_FatePointTransaction_TheologyRecordId",
                table: "FatePointTransaction",
                newName: "IX_FatePointTransaction_ServicePriceId");

            migrationBuilder.AddForeignKey(
                name: "FK_FatePointTransaction_ServicePrice_ServicePriceId",
                table: "FatePointTransaction",
                column: "ServicePriceId",
                principalTable: "ServicePrice",
                principalColumn: "Id");
        }
    }
}

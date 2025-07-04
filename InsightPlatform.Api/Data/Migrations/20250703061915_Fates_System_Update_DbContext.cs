using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightPlatform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fates_System_Update_DbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FatePointTransaction_TheologyRecords_TheologyRecordId",
                table: "FatePointTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_FatePointTransaction_Transaction_TransactionId",
                table: "FatePointTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_FatePointTransaction_Users_UserId",
                table: "FatePointTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_TheologyRecords_ServicePrice_ServicePriceId",
                table: "TheologyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_TopUpPackage_TopUpPackageId",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Users_UserId",
                table: "Transaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transaction",
                table: "Transaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TopUpPackage",
                table: "TopUpPackage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServicePrice",
                table: "ServicePrice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FatePointTransaction",
                table: "FatePointTransaction");

            migrationBuilder.RenameTable(
                name: "Transaction",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "TopUpPackage",
                newName: "TopUpPackages");

            migrationBuilder.RenameTable(
                name: "ServicePrice",
                newName: "ServicePrices");

            migrationBuilder.RenameTable(
                name: "FatePointTransaction",
                newName: "FatePointTransactions");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_UserId",
                table: "Transactions",
                newName: "IX_Transactions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_TopUpPackageId",
                table: "Transactions",
                newName: "IX_Transactions_TopUpPackageId");

            migrationBuilder.RenameIndex(
                name: "IX_FatePointTransaction_UserId",
                table: "FatePointTransactions",
                newName: "IX_FatePointTransactions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FatePointTransaction_TransactionId",
                table: "FatePointTransactions",
                newName: "IX_FatePointTransactions_TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_FatePointTransaction_TheologyRecordId",
                table: "FatePointTransactions",
                newName: "IX_FatePointTransactions_TheologyRecordId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopUpPackages",
                table: "TopUpPackages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServicePrices",
                table: "ServicePrices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FatePointTransactions",
                table: "FatePointTransactions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FatePointTransactions_TheologyRecords_TheologyRecordId",
                table: "FatePointTransactions",
                column: "TheologyRecordId",
                principalTable: "TheologyRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FatePointTransactions_Transactions_TransactionId",
                table: "FatePointTransactions",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FatePointTransactions_Users_UserId",
                table: "FatePointTransactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TheologyRecords_ServicePrices_ServicePriceId",
                table: "TheologyRecords",
                column: "ServicePriceId",
                principalTable: "ServicePrices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_TopUpPackages_TopUpPackageId",
                table: "Transactions",
                column: "TopUpPackageId",
                principalTable: "TopUpPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FatePointTransactions_TheologyRecords_TheologyRecordId",
                table: "FatePointTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FatePointTransactions_Transactions_TransactionId",
                table: "FatePointTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FatePointTransactions_Users_UserId",
                table: "FatePointTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_TheologyRecords_ServicePrices_ServicePriceId",
                table: "TheologyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_TopUpPackages_TopUpPackageId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TopUpPackages",
                table: "TopUpPackages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServicePrices",
                table: "ServicePrices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FatePointTransactions",
                table: "FatePointTransactions");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Transaction");

            migrationBuilder.RenameTable(
                name: "TopUpPackages",
                newName: "TopUpPackage");

            migrationBuilder.RenameTable(
                name: "ServicePrices",
                newName: "ServicePrice");

            migrationBuilder.RenameTable(
                name: "FatePointTransactions",
                newName: "FatePointTransaction");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_UserId",
                table: "Transaction",
                newName: "IX_Transaction_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_TopUpPackageId",
                table: "Transaction",
                newName: "IX_Transaction_TopUpPackageId");

            migrationBuilder.RenameIndex(
                name: "IX_FatePointTransactions_UserId",
                table: "FatePointTransaction",
                newName: "IX_FatePointTransaction_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FatePointTransactions_TransactionId",
                table: "FatePointTransaction",
                newName: "IX_FatePointTransaction_TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_FatePointTransactions_TheologyRecordId",
                table: "FatePointTransaction",
                newName: "IX_FatePointTransaction_TheologyRecordId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transaction",
                table: "Transaction",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopUpPackage",
                table: "TopUpPackage",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServicePrice",
                table: "ServicePrice",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FatePointTransaction",
                table: "FatePointTransaction",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FatePointTransaction_TheologyRecords_TheologyRecordId",
                table: "FatePointTransaction",
                column: "TheologyRecordId",
                principalTable: "TheologyRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FatePointTransaction_Transaction_TransactionId",
                table: "FatePointTransaction",
                column: "TransactionId",
                principalTable: "Transaction",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FatePointTransaction_Users_UserId",
                table: "FatePointTransaction",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TheologyRecords_ServicePrice_ServicePriceId",
                table: "TheologyRecords",
                column: "ServicePriceId",
                principalTable: "ServicePrice",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_TopUpPackage_TopUpPackageId",
                table: "Transaction",
                column: "TopUpPackageId",
                principalTable: "TopUpPackage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Users_UserId",
                table: "Transaction",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

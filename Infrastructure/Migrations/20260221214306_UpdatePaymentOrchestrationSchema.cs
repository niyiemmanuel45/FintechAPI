using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentOrchestrationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WebhookEvents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderEventId",
                table: "WebhookEvents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "WebhookEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderTransactionId",
                table: "PaymentTransactions",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKeyValue",
                table: "IdempotencyKeys",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_ProviderEventId",
                table: "WebhookEvents",
                column: "ProviderEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_Status_NextRetryAt",
                table: "WebhookEvents",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CurrentState",
                table: "PaymentTransactions",
                column: "CurrentState");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Provider_CreatedAt",
                table: "PaymentTransactions",
                columns: new[] { "Provider", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ProviderTransactionId",
                table: "PaymentTransactions",
                column: "ProviderTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_TransactionNumber",
                table: "PaymentTransactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserId_CreatedAt",
                table: "PaymentTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStateHistory_PaymentTransactionId",
                table: "PaymentStateHistory",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKeys_ExpiresAt",
                table: "IdempotencyKeys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKeys_IdempotencyKeyValue",
                table: "IdempotencyKeys",
                column: "IdempotencyKeyValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UserId_IdempotencyKeyValue",
                table: "IdempotencyKeys",
                columns: new[] { "UserId", "IdempotencyKeyValue" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentStateHistory_PaymentTransactions_PaymentTransactionId",
                table: "PaymentStateHistory",
                column: "PaymentTransactionId",
                principalTable: "PaymentTransactions",
                principalColumn: "PaymentTransactionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentStateHistory_PaymentTransactions_PaymentTransactionId",
                table: "PaymentStateHistory");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_ProviderEventId",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_Status_NextRetryAt",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_CurrentState",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_Provider_CreatedAt",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_ProviderTransactionId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_TransactionNumber",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_UserId_CreatedAt",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentStateHistory_PaymentTransactionId",
                table: "PaymentStateHistory");

            migrationBuilder.DropIndex(
                name: "IX_IdempotencyKeys_ExpiresAt",
                table: "IdempotencyKeys");

            migrationBuilder.DropIndex(
                name: "IX_IdempotencyKeys_IdempotencyKeyValue",
                table: "IdempotencyKeys");

            migrationBuilder.DropIndex(
                name: "UQ_UserId_IdempotencyKeyValue",
                table: "IdempotencyKeys");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WebhookEvents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderEventId",
                table: "WebhookEvents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "WebhookEvents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderTransactionId",
                table: "PaymentTransactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKeyValue",
                table: "IdempotencyKeys",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);
        }
    }
}

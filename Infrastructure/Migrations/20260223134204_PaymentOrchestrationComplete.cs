using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentOrchestrationComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResourceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationReports",
                columns: table => new
                {
                    ReconciliationReportId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalInternalTransactions = table.Column<int>(type: "int", nullable: false),
                    TotalProviderTransactions = table.Column<int>(type: "int", nullable: false),
                    MatchedTransactions = table.Column<int>(type: "int", nullable: false),
                    MismatchedTransactions = table.Column<int>(type: "int", nullable: false),
                    InternalOnlyTransactions = table.Column<int>(type: "int", nullable: false),
                    ProviderOnlyTransactions = table.Column<int>(type: "int", nullable: false),
                    TotalInternalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalProviderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalMatchedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalMismatchedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationReports", x => x.ReconciliationReportId);
                });

            migrationBuilder.CreateTable(
                name: "SecretRotationHistory",
                columns: table => new
                {
                    RotationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecretName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecretType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldSecretHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewSecretHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RotatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RotatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RotationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretRotationHistory", x => x.RotationId);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditLogs",
                columns: table => new
                {
                    SecurityAuditLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResourceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThreatIndicators = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresInvestigation = table.Column<bool>(type: "bit", nullable: false),
                    IsInvestigated = table.Column<bool>(type: "bit", nullable: false),
                    InvestigatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvestigatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvestigationNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditLogs", x => x.SecurityAuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "TransactionMismatches",
                columns: table => new
                {
                    MismatchId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReconciliationReportId = table.Column<long>(type: "bigint", nullable: false),
                    MismatchType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InternalTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    InternalTransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    InternalStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderTransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProviderStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionMismatches", x => x.MismatchId);
                    table.ForeignKey(
                        name: "FK_TransactionMismatches_ReconciliationReports_ReconciliationReportId",
                        column: x => x.ReconciliationReportId,
                        principalTable: "ReconciliationReports",
                        principalColumn: "ReconciliationReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category",
                table: "AuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EventType",
                table: "AuditLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PaymentTransactionId",
                table: "AuditLogs",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp_EventType",
                table: "AuditLogs",
                columns: new[] { "Timestamp", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationReports_PeriodStart_PeriodEnd",
                table: "ReconciliationReports",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationReports_ReportDate",
                table: "ReconciliationReports",
                column: "ReportDate");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationReports_Status",
                table: "ReconciliationReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SecretRotationHistory_RotatedAt",
                table: "SecretRotationHistory",
                column: "RotatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecretRotationHistory_SecretName",
                table: "SecretRotationHistory",
                column: "SecretName");

            migrationBuilder.CreateIndex(
                name: "IX_SecretRotationHistory_SecretName_IsActive",
                table: "SecretRotationHistory",
                columns: new[] { "SecretName", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SecretRotationHistory_Status",
                table: "SecretRotationHistory",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_EventType",
                table: "SecurityAuditLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_IpAddress",
                table: "SecurityAuditLogs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_RequiresInvestigation",
                table: "SecurityAuditLogs",
                column: "RequiresInvestigation");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_RequiresInvestigation_IsInvestigated",
                table: "SecurityAuditLogs",
                columns: new[] { "RequiresInvestigation", "IsInvestigated" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_Timestamp",
                table: "SecurityAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_Timestamp_EventType",
                table: "SecurityAuditLogs",
                columns: new[] { "Timestamp", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_UserId",
                table: "SecurityAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_UserId_Timestamp",
                table: "SecurityAuditLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionMismatches_DetectedAt",
                table: "TransactionMismatches",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionMismatches_InternalTransactionId",
                table: "TransactionMismatches",
                column: "InternalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionMismatches_MismatchType",
                table: "TransactionMismatches",
                column: "MismatchType");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionMismatches_ReconciliationReportId",
                table: "TransactionMismatches",
                column: "ReconciliationReportId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionMismatches_ResolutionStatus",
                table: "TransactionMismatches",
                column: "ResolutionStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "SecretRotationHistory");

            migrationBuilder.DropTable(
                name: "SecurityAuditLogs");

            migrationBuilder.DropTable(
                name: "TransactionMismatches");

            migrationBuilder.DropTable(
                name: "ReconciliationReports");
        }
    }
}

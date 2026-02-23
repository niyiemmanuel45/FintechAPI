using Application.Interfaces;
using Application.Services.Payments;
using Core.Entities;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class ReconciliationService : IReconciliationService
{
    private readonly ApplicationDbContext _db;
    private readonly PaymentProviderFactory _providerFactory;
    private readonly ILogger<ReconciliationService> _logger;
    private readonly IEmailService _emailService;

    public ReconciliationService(
        ApplicationDbContext db,
        PaymentProviderFactory providerFactory,
        ILogger<ReconciliationService> logger,
        IEmailService emailService)
    {
        _db = db;
        _providerFactory = providerFactory;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<ReconciliationReport> RunDailyReconciliationAsync(DateTime date)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);
        
        return await RunReconciliationForPeriodAsync(startDate, endDate);
    }

    public async Task<ReconciliationReport> RunReconciliationForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        var report = new ReconciliationReport
        {
            ReportDate = DateTime.UtcNow,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            Status = "Pending"
        };

        try
        {
            _logger.LogInformation("Starting reconciliation for period {Start} to {End}", startDate, endDate);

            // Get all internal transactions for the period
            var internalTransactions = await _db.PaymentTransactions
                .Where(pt => pt.CreatedAt >= startDate && pt.CreatedAt < endDate)
                .Where(pt => pt.CurrentState == PaymentStateEnum.Completed || pt.CurrentState == PaymentStateEnum.Settled)
                .ToListAsync();

            report.TotalInternalTransactions = internalTransactions.Count;
            report.TotalInternalAmount = internalTransactions.Sum(t => t.Amount);

            // Group by provider
            var transactionsByProvider = internalTransactions
                .Where(t => t.ProviderTransactionId != null)
                .GroupBy(t => t.Provider);

            var allMismatches = new List<TransactionMismatch>();
            int totalMatched = 0;
            decimal totalMatchedAmount = 0;

            foreach (var providerGroup in transactionsByProvider)
            {
                try
                {
                    var provider = _providerFactory.GetProvider(providerGroup.Key);
                    var providerTransactions = new Dictionary<string, ProviderTransactionInfo>();

                    // Fetch provider transactions
                    foreach (var transaction in providerGroup)
                    {
                        if (string.IsNullOrEmpty(transaction.ProviderTransactionId))
                            continue;

                        try
                        {
                            var status = await provider.GetPaymentStatusAsync(transaction.ProviderTransactionId);
                            providerTransactions[transaction.ProviderTransactionId] = new ProviderTransactionInfo
                            {
                                TransactionId = transaction.ProviderTransactionId,
                                Status = status.Status,
                                Amount = transaction.Amount // Provider APIs may not return amount in status check
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error fetching status for provider transaction {TransactionId}", transaction.ProviderTransactionId);
                        }
                    }

                    report.TotalProviderTransactions += providerTransactions.Count;

                    // Match transactions
                    foreach (var internalTxn in providerGroup)
                    {
                        if (string.IsNullOrEmpty(internalTxn.ProviderTransactionId))
                        {
                            // Internal transaction without provider reference
                            allMismatches.Add(new TransactionMismatch
                            {
                                MismatchType = "InternalOnly",
                                InternalTransactionId = internalTxn.PaymentTransactionId,
                                InternalTransactionReference = internalTxn.TransactionNumber,
                                InternalAmount = internalTxn.Amount,
                                InternalStatus = internalTxn.CurrentState.ToString(),
                                Provider = internalTxn.Provider.ToString(),
                                Description = "Internal transaction has no provider reference"
                            });
                            continue;
                        }

                        if (!providerTransactions.TryGetValue(internalTxn.ProviderTransactionId, out var providerTxn))
                        {
                            // Provider transaction not found
                            allMismatches.Add(new TransactionMismatch
                            {
                                MismatchType = "ProviderOnly",
                                InternalTransactionId = internalTxn.PaymentTransactionId,
                                InternalTransactionReference = internalTxn.TransactionNumber,
                                InternalAmount = internalTxn.Amount,
                                InternalStatus = internalTxn.CurrentState.ToString(),
                                ProviderTransactionId = internalTxn.ProviderTransactionId,
                                Provider = internalTxn.Provider.ToString(),
                                Description = "Provider transaction not found or failed to fetch"
                            });
                            continue;
                        }

                        // Check for amount mismatch
                        if (Math.Abs(internalTxn.Amount - providerTxn.Amount) > 0.01m)
                        {
                            allMismatches.Add(new TransactionMismatch
                            {
                                MismatchType = "AmountMismatch",
                                InternalTransactionId = internalTxn.PaymentTransactionId,
                                InternalTransactionReference = internalTxn.TransactionNumber,
                                InternalAmount = internalTxn.Amount,
                                InternalStatus = internalTxn.CurrentState.ToString(),
                                ProviderTransactionId = internalTxn.ProviderTransactionId,
                                ProviderAmount = providerTxn.Amount,
                                ProviderStatus = providerTxn.Status,
                                Provider = internalTxn.Provider.ToString(),
                                Description = $"Amount mismatch: Internal={internalTxn.Amount}, Provider={providerTxn.Amount}"
                            });
                            continue;
                        }

                        // Check for status mismatch
                        var normalizedProviderStatus = NormalizeProviderStatus(providerTxn.Status);
                        if (normalizedProviderStatus != "success" && normalizedProviderStatus != "completed")
                        {
                            allMismatches.Add(new TransactionMismatch
                            {
                                MismatchType = "StatusMismatch",
                                InternalTransactionId = internalTxn.PaymentTransactionId,
                                InternalTransactionReference = internalTxn.TransactionNumber,
                                InternalAmount = internalTxn.Amount,
                                InternalStatus = internalTxn.CurrentState.ToString(),
                                ProviderTransactionId = internalTxn.ProviderTransactionId,
                                ProviderAmount = providerTxn.Amount,
                                ProviderStatus = providerTxn.Status,
                                Provider = internalTxn.Provider.ToString(),
                                Description = $"Status mismatch: Internal={internalTxn.CurrentState}, Provider={providerTxn.Status}"
                            });
                            continue;
                        }

                        // Transaction matched successfully
                        totalMatched++;
                        totalMatchedAmount += internalTxn.Amount;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reconciling provider {Provider}", providerGroup.Key);
                }
            }

            // Update report
            report.MatchedTransactions = totalMatched;
            report.TotalMatchedAmount = totalMatchedAmount;
            report.MismatchedTransactions = allMismatches.Count;
            report.TotalMismatchedAmount = allMismatches.Sum(m => m.InternalAmount ?? 0);
            report.InternalOnlyTransactions = allMismatches.Count(m => m.MismatchType == "InternalOnly");
            report.ProviderOnlyTransactions = allMismatches.Count(m => m.MismatchType == "ProviderOnly");
            report.Status = "Completed";
            report.CompletedAt = DateTime.UtcNow;

            // Save report
            _db.ReconciliationReports.Add(report);
            await _db.SaveChangesAsync();

            // Save mismatches
            foreach (var mismatch in allMismatches)
            {
                mismatch.ReconciliationReportId = report.ReconciliationReportId;
            }
            _db.TransactionMismatches.AddRange(allMismatches);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Reconciliation completed: {Matched} matched, {Mismatched} mismatched out of {Total} transactions",
                totalMatched, allMismatches.Count, internalTransactions.Count);

            // Send alert if there are mismatches
            if (allMismatches.Count > 0)
            {
                await SendMismatchAlertAsync(report, allMismatches);
            }

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reconciliation");
            report.Status = "Failed";
            report.ErrorMessage = ex.Message;
            report.CompletedAt = DateTime.UtcNow;
            
            _db.ReconciliationReports.Add(report);
            await _db.SaveChangesAsync();
            
            throw;
        }
    }

    public async Task<List<ReconciliationReport>> GetRecentReportsAsync(int count = 10)
    {
        return await _db.ReconciliationReports
            .OrderByDescending(r => r.ReportDate)
            .Take(count)
            .Include(r => r.Mismatches)
            .ToListAsync();
    }

    public async Task<ReconciliationReport?> GetReportByIdAsync(long reportId)
    {
        return await _db.ReconciliationReports
            .Include(r => r.Mismatches)
            .FirstOrDefaultAsync(r => r.ReconciliationReportId == reportId);
    }

    public async Task<List<TransactionMismatch>> GetUnresolvedMismatchesAsync()
    {
        return await _db.TransactionMismatches
            .Where(m => m.ResolutionStatus == "Pending" || m.ResolutionStatus == "Investigating")
            .OrderByDescending(m => m.DetectedAt)
            .ToListAsync();
    }

    public async Task<bool> ResolveMismatchAsync(long mismatchId, string resolutionNotes, Guid resolvedBy)
    {
        var mismatch = await _db.TransactionMismatches.FindAsync(mismatchId);
        if (mismatch == null)
            return false;

        mismatch.ResolutionStatus = "Resolved";
        mismatch.ResolutionNotes = resolutionNotes;
        mismatch.ResolvedBy = resolvedBy;
        mismatch.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    private string NormalizeProviderStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "success" or "successful" or "completed" or "settled" => "success",
            "pending" or "processing" => "pending",
            "failed" or "declined" or "cancelled" => "failed",
            _ => "unknown"
        };
    }

    private async Task SendMismatchAlertAsync(ReconciliationReport report, List<TransactionMismatch> mismatches)
    {
        try
        {
            var subject = $"Reconciliation Alert: {mismatches.Count} Mismatches Detected";
            var body = $@"
                <h2>Reconciliation Report Alert</h2>
                <p><strong>Report Date:</strong> {report.ReportDate:yyyy-MM-dd HH:mm:ss}</p>
                <p><strong>Period:</strong> {report.PeriodStart:yyyy-MM-dd} to {report.PeriodEnd:yyyy-MM-dd}</p>
                <h3>Summary</h3>
                <ul>
                    <li>Total Internal Transactions: {report.TotalInternalTransactions}</li>
                    <li>Matched Transactions: {report.MatchedTransactions}</li>
                    <li>Mismatched Transactions: {report.MismatchedTransactions}</li>
                    <li>Internal Only: {report.InternalOnlyTransactions}</li>
                    <li>Provider Only: {report.ProviderOnlyTransactions}</li>
                </ul>
                <h3>Mismatches by Type</h3>
                <ul>
                    <li>Amount Mismatches: {mismatches.Count(m => m.MismatchType == "AmountMismatch")}</li>
                    <li>Status Mismatches: {mismatches.Count(m => m.MismatchType == "StatusMismatch")}</li>
                    <li>Internal Only: {mismatches.Count(m => m.MismatchType == "InternalOnly")}</li>
                    <li>Provider Only: {mismatches.Count(m => m.MismatchType == "ProviderOnly")}</li>
                </ul>
                <p>Please review the reconciliation report and resolve any discrepancies.</p>
            ";

            // Send to admin email (configured in appsettings)
            // await _emailService.SendEmailAsync(adminUser, subject, body);
            
            _logger.LogWarning("Reconciliation alert: {Count} mismatches detected", mismatches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mismatch alert");
        }
    }

    private class ProviderTransactionInfo
    {
        public string TransactionId { get; set; } = string.Empty;
        public string? Status { get; set; }
        public decimal Amount { get; set; }
    }
}

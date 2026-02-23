using Core.Entities;

namespace Application.Interfaces;

public interface IReconciliationService
{
    Task<ReconciliationReport> RunDailyReconciliationAsync(DateTime date);
    Task<ReconciliationReport> RunReconciliationForPeriodAsync(DateTime startDate, DateTime endDate);
    Task<List<ReconciliationReport>> GetRecentReportsAsync(int count = 10);
    Task<ReconciliationReport?> GetReportByIdAsync(long reportId);
    Task<List<TransactionMismatch>> GetUnresolvedMismatchesAsync();
    Task<bool> ResolveMismatchAsync(long mismatchId, string resolutionNotes, Guid resolvedBy);
}

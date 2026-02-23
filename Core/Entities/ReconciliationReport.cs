using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

[Table("ReconciliationReports")]
public class ReconciliationReport
{
    [Key]
    public long ReconciliationReportId { get; set; }
    
    public DateTime ReportDate { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    public int TotalInternalTransactions { get; set; }
    public int TotalProviderTransactions { get; set; }
    public int MatchedTransactions { get; set; }
    public int MismatchedTransactions { get; set; }
    public int InternalOnlyTransactions { get; set; }
    public int ProviderOnlyTransactions { get; set; }
    
    public decimal TotalInternalAmount { get; set; }
    public decimal TotalProviderAmount { get; set; }
    public decimal TotalMatchedAmount { get; set; }
    public decimal TotalMismatchedAmount { get; set; }
    
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    public ICollection<TransactionMismatch> Mismatches { get; set; } = new List<TransactionMismatch>();
}

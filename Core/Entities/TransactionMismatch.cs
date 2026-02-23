using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

[Table("TransactionMismatches")]
public class TransactionMismatch
{
    [Key]
    public long MismatchId { get; set; }
    
    public long ReconciliationReportId { get; set; }
    public ReconciliationReport ReconciliationReport { get; set; }
    
    public string MismatchType { get; set; } // InternalOnly, ProviderOnly, AmountMismatch, StatusMismatch
    
    public long? InternalTransactionId { get; set; }
    public string? InternalTransactionReference { get; set; }
    public decimal? InternalAmount { get; set; }
    public string? InternalStatus { get; set; }
    
    public string? ProviderTransactionId { get; set; }
    public string? ProviderTransactionReference { get; set; }
    public decimal? ProviderAmount { get; set; }
    public string? ProviderStatus { get; set; }
    
    public string? Provider { get; set; }
    public string? Description { get; set; }
    
    public string ResolutionStatus { get; set; } = "Pending"; // Pending, Investigating, Resolved, Ignored
    public string? ResolutionNotes { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

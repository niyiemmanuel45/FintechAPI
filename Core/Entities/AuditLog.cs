using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public long AuditLogId { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string EventType { get; set; } = string.Empty; // PaymentInitiated, PaymentCompleted, StateTransition, etc.
    public string Category { get; set; } = string.Empty; // Payment, Security, System, User
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical
    
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    public long? PaymentTransactionId { get; set; }
    public string? TransactionReference { get; set; }
    public string? Provider { get; set; }
    
    public string? Action { get; set; }
    public string? Resource { get; set; }
    public string? ResourceId { get; set; }
    
    public string? OldValue { get; set; } // Encrypted if sensitive
    public string? NewValue { get; set; } // Encrypted if sensitive
    
    public string? Details { get; set; } // JSON with additional context
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    
    public bool IsSuccess { get; set; } = true;
    public bool IsSensitive { get; set; } = false;
    
    public string? CorrelationId { get; set; } // For tracking related events
    public string? SessionId { get; set; }
}

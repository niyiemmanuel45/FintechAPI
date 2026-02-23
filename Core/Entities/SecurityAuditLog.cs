using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

[Table("SecurityAuditLogs")]
public class SecurityAuditLog
{
    [Key]
    public long SecurityAuditLogId { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string EventType { get; set; } = string.Empty; // Login, Logout, FailedLogin, PasswordChange, SecretRotation, etc.
    public string Severity { get; set; } = "Info"; // Info, Warning, Critical
    
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; } // Geolocation if available
    
    public string? Action { get; set; }
    public string? Resource { get; set; }
    public string? ResourceId { get; set; }
    
    public bool IsSuccess { get; set; } = true;
    public string? FailureReason { get; set; }
    
    public string? Details { get; set; } // JSON with additional context (encrypted)
    public string? ThreatIndicators { get; set; } // Suspicious patterns detected
    
    public bool RequiresInvestigation { get; set; } = false;
    public bool IsInvestigated { get; set; } = false;
    public Guid? InvestigatedBy { get; set; }
    public DateTime? InvestigatedAt { get; set; }
    public string? InvestigationNotes { get; set; }
    
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
}

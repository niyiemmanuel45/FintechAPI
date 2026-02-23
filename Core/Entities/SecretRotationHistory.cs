using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

[Table("SecretRotationHistory")]
public class SecretRotationHistory
{
    [Key]
    public long RotationId { get; set; }
    
    public string SecretName { get; set; } = string.Empty;
    public string SecretType { get; set; } = string.Empty; // ApiKey, WebhookSecret, JwtKey, etc.
    public string? Provider { get; set; } // Paystack, Flutterwave, Remita, etc.
    
    public string? OldSecretHash { get; set; } // SHA256 hash of old secret (for audit)
    public string? NewSecretHash { get; set; } // SHA256 hash of new secret (for audit)
    
    public DateTime RotatedAt { get; set; } = DateTime.UtcNow;
    public Guid? RotatedBy { get; set; } // User who triggered rotation (null for automatic)
    public string RotationType { get; set; } = "Automatic"; // Automatic, Manual, Emergency
    
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, RolledBack
    public string? ErrorMessage { get; set; }
    
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveUntil { get; set; }
    
    public bool IsActive { get; set; } = true;
}

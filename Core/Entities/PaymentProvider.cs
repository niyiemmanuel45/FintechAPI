using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Entities;

[Table("PaymentProviders")]
public class PaymentProvider
{
    [Key]
    public int ProviderId { get; set; }
    public string Name { get; set; }
    public EnumPaymentGateway Gateway { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public string ApiKey { get; set; }
    public string WebhookSecret { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

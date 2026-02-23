using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

[Table("WebhookEvents")]
public class WebhookEvent
{
    public long WebhookEventId { get; set; }
    public string Provider { get; set; }
    public string ProviderEventId { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public string Signature { get; set; }
    public string Status { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

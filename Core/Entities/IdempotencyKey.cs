using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

[Table("IdempotencyKeys")]
public class IdempotencyKey
{
    public long IdempotencyKeyId { get; set; }
    public Guid UserId { get; set; }
    public string TransactionId { get; set; }
    public string IdempotencyKeyValue { get; set; }
    public string RequestHash { get; set; }
    public string? ResponseHash { get; set; }
    public string? ResponseData { get; set; }
    public string Status { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Entities;

[Table("PaymentStateHistory")]
public class PaymentStateHistory
{
    [Key]
    public long StateHistoryId { get; set; }
    public long PaymentTransactionId { get; set; }
    public PaymentTransaction PaymentTransaction { get; set; }
    public PaymentStateEnum FromState { get; set; }
    public PaymentStateEnum ToState { get; set; }
    public DateTime TransitionedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public Guid? TransitionedBy { get; set; }
}
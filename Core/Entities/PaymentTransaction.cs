using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

[Table("PaymentTransactions")]
public class PaymentTransaction
{
    public long PaymentTransactionId { get; set; }
    public string TransactionNumber { get; set; }
    public Guid UserId { get; set; }
    public int BankAccountId { get; set; }

    public decimal Amount { get; set; }
    public EnumCurrency Currency { get; set; }
    public string? PaymentMethod { get; set; }

    public EnumPaymentGateway Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? ProviderResponse { get; set; }
    public DateTime? ProviderConfirmedAt { get; set; }

    public long? IdempotencyKeyId { get; set; }

    public PaymentStateEnum CurrentState { get; set; }
    public PaymentStateEnum? PreviousState { get; set; }
    public ICollection<PaymentStateHistory> StateHistory { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
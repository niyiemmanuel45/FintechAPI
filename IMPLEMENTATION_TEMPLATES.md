# Payment Orchestration - Implementation Templates

This document provides ready-to-use code templates and structures for implementing the payment orchestration system.

---

## 1. Core Entities & DTOs

### PaymentTransaction Entity

```csharp
using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;

[Table("PaymentTransactions")]
public class PaymentTransaction : IAuditableEntity
{
    // Primary Identifiers
    public long PaymentTransactionId { get; set; }
    public string TransactionNumber { get; set; } // Unique reference for customer

    // User & Account Reference
    public Guid UserId { get; set; }
    public int BankAccountId { get; set; }
    public User User { get; set; }
    public BankAccount BankAccount { get; set; }

    // Payment Details
    public decimal Amount { get; set; }
    public EnumCurrency Currency { get; set; }
    public EnumPaymentMethod PaymentMethod { get; set; }
    public EnumPaymentGateway Provider { get; set; }
    public string? Description { get; set; }

    // Provider Tracking
    public string? ProviderTransactionId { get; set; }
    public DateTime? ProviderConfirmedAt { get; set; }
    public string? ProviderResponse { get; set; } // Encrypted

    // Idempotency
    public long? IdempotencyKeyId { get; set; }
    public IdempotencyKey? IdempotencyKey { get; set; }

    // State Management
    public PaymentStateEnum CurrentState { get; set; }
    public PaymentStateEnum? PreviousState { get; set; }
    public ICollection<PaymentStateHistory> StateHistory { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Audit
    public string? InitiatedIpAddress { get; set; }
    public int? RetryCount { get; set; } = 0;

    [Computed]
    public bool IsCompleted => CurrentState == PaymentStateEnum.Completed;

    [Computed]
    public bool IsFailed => CurrentState == PaymentStateEnum.Failed
        || CurrentState == PaymentStateEnum.Declined;
}

public class PaymentStateHistory
{
    public long StateHistoryId { get; set; }
    public long PaymentTransactionId { get; set; }
    public PaymentTransaction PaymentTransaction { get; set; }

    public PaymentStateEnum FromState { get; set; }
    public PaymentStateEnum ToState { get; set; }
    public DateTime TransitionedAt { get; set; }
    public string? Reason { get; set; }
    public Guid? TransitionedBy { get; set; }
}
```

### Idempotency Key Entity

```csharp
namespace Core.Entities;

[Table("IdempotencyKeys")]
public class IdempotencyKey
{
    public long IdempotencyKeyId { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }

    public string TransactionId { get; set; }
    public string IdempotencyKeyValue { get; set; }

    public string RequestHash { get; set; }
    public string? ResponseHash { get; set; }
    public string? ResponseData { get; set; }

    public IdempotencyStatus Status { get; set; }
    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public enum IdempotencyStatus
{
    Pending,
    Completed,
    Failed
}
```

### Webhook Event Entity

```csharp
namespace Core.Entities;

[Table("WebhookEvents")]
public class WebhookEvent
{
    public long WebhookEventId { get; set; }

    public string Provider { get; set; }
    public string ProviderEventId { get; set; }
    public string EventType { get; set; }

    public string Payload { get; set; } // JSON payload
    public string Signature { get; set; }

    public WebhookStatus Status { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public DateTime ReceivedAt { get; set; }

    // Navigation
    public long? PaymentTransactionId { get; set; }
    public PaymentTransaction? PaymentTransaction { get; set; }
}

public enum WebhookStatus
{
    Pending,
    Processing,
    Processed,
    PendingRetry,
    Failed,
    DeadLettered
}
```

### Audit Log Entity

```csharp
namespace Core.Entities;

[Table("AuditLogs")]
public class AuditLog
{
    public long AuditLogId { get; set; }

    public string? TransactionId { get; set; }
    public string EventType { get; set; }
    public Guid? UserId { get; set; }

    public string Details { get; set; } // Encrypted
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; }
}

[Table("SecurityAuditLogs")]
public class SecurityAuditLog
{
    public long SecurityAuditLogId { get; set; }

    public string EventType { get; set; }
    public string Details { get; set; } // Encrypted
    public SecuritySeverity Severity { get; set; }

    public DateTime Timestamp { get; set; }
}

public enum SecuritySeverity
{
    Info,
    Warning,
    Critical
}
```

### Payment Provider Entity

```csharp
namespace Core.Entities;

[Table("PaymentProviders")]
public class PaymentProvider
{
    public int ProviderId { get; set; }

    public string Name { get; set; }
    public EnumPaymentGateway Gateway { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; } // For failover routing

    public string ApiKey { get; set; } // Encrypted
    public string WebhookSecret { get; set; } // Encrypted
    public string? ApiUrl { get; set; }

    public DateTime SecretRotatedAt { get; set; }
    public DateTime? NextSecretRotationAt { get; set; }

    public string? Configuration { get; set; } // JSON

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum EnumPaymentGateway
{
    Stripe,
    Paystack,
    Flutterwave,
    PayPal,
    Mock // For testing
}
```

### Reconciliation Entities

```csharp
namespace Core.Entities;

[Table("ReconciliationReports")]
public class ReconciliationReport
{
    public long ReconciliationReportId { get; set; }

    public DateTime ReportDate { get; set; }
    public DateTime GeneratedAt { get; set; }

    public decimal TotalAmount { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }

    public bool HasMismatches { get; set; }

    public ICollection<ProviderReconciliation> ProviderReconciliations { get; set; }
}

[Table("ProviderReconciliations")]
public class ProviderReconciliation
{
    public long ProviderReconciliationId { get; set; }
    public long ReconciliationReportId { get; set; }

    public string Provider { get; set; }
    public int InternalTransactionCount { get; set; }
    public int ProviderTransactionCount { get; set; }
    public int MatchedCount { get; set; }

    public bool HasMismatches { get; set; }
    public string? MismatchDetails { get; set; } // JSON

    public ReconciliationReport ReconciliationReport { get; set; }
}

[Table("TransactionMismatches")]
public class TransactionMismatch
{
    public long MismatchId { get; set; }
    public long ProviderReconciliationId { get; set; }

    public long? InternalTransactionId { get; set; }
    public string? ProviderTransactionId { get; set; }

    public string MismatchReason { get; set; }
    public string? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }

    public ProviderReconciliation ProviderReconciliation { get; set; }
}
```

---

## 2. DTOs for Payment Orchestration

### Payment Request DTO

```csharp
namespace Application.DTOs.Payments;

public class PaymentInitiationRequest
{
    [Required]
    public decimal Amount { get; set; }

    [Required]
    public EnumCurrency Currency { get; set; }

    [Required]
    public int CardId { get; set; }

    [Required]
    public EnumPaymentMethod PaymentMethod { get; set; }

    public string? Description { get; set; }

    [Required]
    public string IdempotencyKey { get; set; } // UUID format

    public Dictionary<string, string>? Metadata { get; set; }
}

public class PaymentResponse
{
    public long PaymentTransactionId { get; set; }
    public string TransactionNumber { get; set; }

    public decimal Amount { get; set; }
    public EnumCurrency Currency { get; set; }

    public PaymentStateEnum CurrentState { get; set; }
    public string? ProviderTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
}

public class PaymentStatusResponse
{
    public long PaymentTransactionId { get; set; }
    public PaymentStateEnum CurrentState { get; set; }
    public PaymentStateEnum? PreviousState { get; set; }

    public List<PaymentStateHistoryDto> StateHistory { get; set; }

    public string? ProviderTransactionId { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class PaymentStateHistoryDto
{
    public PaymentStateEnum FromState { get; set; }
    public PaymentStateEnum ToState { get; set; }
    public DateTime TransitionedAt { get; set; }
    public string? Reason { get; set; }
}
```

### Webhook DTOs

```csharp
namespace Application.DTOs.Webhooks;

public class WebhookProcessingRequest
{
    public string Payload { get; set; }
    public string Signature { get; set; }
    public string Provider { get; set; }
}

public class WebhookProcessingResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public string? ErrorReason { get; set; }
}

// Provider-specific webhook payloads
public class StripeWebhookPayload
{
    [JsonProperty("id")]
    public string EventId { get; set; }

    [JsonProperty("type")]
    public string EventType { get; set; }

    [JsonProperty("data")]
    public object Data { get; set; }

    [JsonProperty("created")]
    public long CreatedTimestamp { get; set; }
}

public class PaystackWebhookPayload
{
    [JsonProperty("event")]
    public string Event { get; set; }

    [JsonProperty("data")]
    public PaystackEventData Data { get; set; }
}

public class PaystackEventData
{
    [JsonProperty("reference")]
    public string Reference { get; set; }

    [JsonProperty("amount")]
    public int Amount { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
}
```

---

## 3. Service Interfaces

### Payment Orchestration Interface

```csharp
namespace Application.Interfaces.Payments;

public interface IPaymentOrchestrationService
{
    /// <summary>
    /// Initiates a payment with idempotency support and failover logic
    /// </summary>
    Task<PaymentResponse> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves current payment status
    /// </summary>
    Task<PaymentStatusResponse> GetPaymentStatusAsync(
        long paymentTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates refund process
    /// </summary>
    Task<RefundResponse> RefundPaymentAsync(
        long paymentTransactionId,
        decimal? amount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available providers based on currency and region
    /// </summary>
    Task<List<PaymentProviderInfo>> GetAvailableProvidersAsync(
        EnumCurrency currency,
        string? region = null);
}

public class PaymentProviderInfo
{
    public EnumPaymentGateway Gateway { get; set; }
    public string DisplayName { get; set; }
    public int Priority { get; set; }
    public List<EnumPaymentMethod> SupportedMethods { get; set; }
    public List<EnumCurrency> SupportedCurrencies { get; set; }
}
```

### Provider Interface

```csharp
namespace Core.Interfaces.Payments;

public interface IPaymentProvider
{
    string ProviderName { get; }

    Task<ProviderPaymentResponse> InitiatePaymentAsync(
        ProviderPaymentRequest request);

    Task<ProviderPaymentStatus> GetPaymentStatusAsync(
        string providerTransactionId);

    Task<ProviderRefundResponse> RefundPaymentAsync(
        string providerTransactionId,
        decimal amount);

    Task<bool> VerifyWebhookSignatureAsync(
        string payload,
        string signature);

    Task<ProviderWebhookEvent> ParseWebhookPayloadAsync(
        string payload);
}

public class ProviderPaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string CustomerEmail { get; set; }
    public string IdempotencyKey { get; set; }
    public string? Metadata { get; set; } // JSON
    public string ReturnUrl { get; set; }
}

public class ProviderPaymentResponse
{
    public bool IsSuccessful { get; set; }
    public string ProviderTransactionId { get; set; }
    public string Status { get; set; }
    public string? Message { get; set; }
    public string? AuthorizationUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Idempotency Service Interface

```csharp
namespace Application.Interfaces.Payments;

public interface IIdempotencyService
{
    /// <summary>
    /// Checks if idempotency key exists and payment is already processed
    /// </summary>
    Task<IdempotencyCheckResult> CheckIdempotencyAsync(
        string idempotencyKey,
        Guid userId);

    /// <summary>
    /// Registers idempotency key for a new payment
    /// </summary>
    Task<IdempotencyKey> RegisterIdempotencyKeyAsync(
        string idempotencyKey,
        Guid userId,
        string transactionId,
        string requestHash);

    /// <summary>
    /// Marks idempotency key as completed with response data
    /// </summary>
    Task MarkAsCompletedAsync(
        string idempotencyKey,
        Guid userId,
        string responseHash,
        object responseData);

    /// <summary>
    /// Marks idempotency key as failed
    /// </summary>
    Task MarkAsFailedAsync(
        string idempotencyKey,
        Guid userId,
        string errorMessage);
}

public class IdempotencyCheckResult
{
    public IdempotencyCheckStatus Status { get; set; }
    public object? CachedResponse { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum IdempotencyCheckStatus
{
    NotFound,
    StillProcessing,
    Completed,
    Failed
}
```

### State Machine Interface

```csharp
namespace Application.Interfaces.Payments;

public interface IPaymentStateTransitionService
{
    /// <summary>
    /// Validates and performs state transition
    /// </summary>
    Task<bool> TransitionStateAsync(
        PaymentTransaction payment,
        PaymentStateEnum newState,
        string reason,
        Guid? transitionedBy = null);

    /// <summary>
    /// Gets allowed transitions from current state
    /// </summary>
    Task<List<PaymentStateEnum>> GetAllowedTransitionsAsync(
        PaymentStateEnum currentState);

    /// <summary>
    /// Gets state transition history
    /// </summary>
    Task<List<PaymentStateHistory>> GetStateHistoryAsync(
        long paymentTransactionId);
}
```

### Webhook Processing Interface

```csharp
namespace Application.Interfaces.Webhooks;

public interface IWebhookProcessingService
{
    /// <summary>
    /// Processes incoming webhook with signature verification
    /// </summary>
    Task<bool> ProcessWebhookAsync(
        string payload,
        string signature,
        string provider);

    /// <summary>
    /// Retrieves webhook event by ID
    /// </summary>
    Task<WebhookEvent> GetWebhookEventAsync(string webhookEventId);

    /// <summary>
    /// Retries failed webhooks with exponential backoff
    /// </summary>
    Task RetryFailedWebhooksAsync();

    /// <summary>
    /// Gets webhook delivery status
    /// </summary>
    Task<WebhookDeliveryStatus> GetDeliveryStatusAsync(
        string webhookEventId);
}

public class WebhookDeliveryStatus
{
    public string WebhookEventId { get; set; }
    public WebhookStatus Status { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? LastError { get; set; }
}
```

### Reconciliation Interface

```csharp
namespace Application.Interfaces.Payments;

public interface IReconciliationService
{
    /// <summary>
    /// Generates daily reconciliation report
    /// </summary>
    Task<ReconciliationReport> GenerateDailyReconciliationAsync(
        DateTime reportDate);

    /// <summary>
    /// Detects mismatches between internal and provider records
    /// </summary>
    Task<List<TransactionMismatch>> DetectMismatchesAsync(
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Resolves a detected mismatch
    /// </summary>
    Task ResolveMismatchAsync(
        long mismatchId,
        string resolution,
        Guid resolvedBy);
}
```

### Audit Service Interface

```csharp
namespace Application.Interfaces.Security;

public interface IAuditLogService
{
    /// <summary>
    /// Logs payment-related event
    /// </summary>
    Task LogPaymentEventAsync(
        string transactionId,
        string eventType,
        string details,
        Guid? userId,
        string? ipAddress);

    /// <summary>
    /// Logs security-related event
    /// </summary>
    Task LogSecurityEventAsync(
        string eventType,
        string details,
        SecuritySeverity severity);

    /// <summary>
    /// Retrieves audit logs
    /// </summary>
    Task<List<AuditLog>> GetAuditLogsAsync(
        string? transactionId = null,
        Guid? userId = null,
        DateTime? startDate = null);
}
```

---

## 4. Enums to Add

```csharp
namespace Core.Enums;

public enum PaymentStateEnum
{
    Pending,                    // Initial state
    Authorized,                 // Payment authorized by provider
    Processing,                 // Payment being processed
    Settled,                    // Payment settled with provider
    Completed,                  // Final successful state
    Failed,                     // Permanent failure
    Declined,                   // Declined by provider
    RequiresAction,             // Requires additional action (3D Secure, etc)
    Cancelled,                  // Cancelled by user or system
    RefundInitiated,            // Refund process started
    RefundProcessing,           // Refund being processed
    Refunded                    // Refund completed
}

public enum EnumPaymentMethod
{
    Card,
    BankTransfer,
    Wallet,
    USSD,
    MobilePayment
}

public enum PaymentGateway
{
    Stripe,
    Paystack,
    Flutterwave,
    PayPal
}
```

---

## 5. Configuration Classes

### Database Context Configuration Examples

```csharp
public class PaymentTransactionEntityConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(p => p.PaymentTransactionId);

        builder.Property(p => p.TransactionNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Amount)
            .HasPrecision(15, 2)
            .IsRequired();

        builder.Property(p => p.ProviderResponse)
            .HasMaxLength(-1);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.IdempotencyKey)
            .WithOne()
            .HasForeignKey<PaymentTransaction>(p => p.IdempotencyKeyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(p => p.TransactionNumber).IsUnique();
        builder.HasIndex(p => p.ProviderTransactionId);
        builder.HasIndex(p => p.CurrentState);
        builder.HasIndex(p => new { p.UserId, p.CreatedAt });
        builder.HasIndex(p => new { p.Provider, p.CreatedAt });
    }
}

public class IdempotencyKeyEntityConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.HasKey(k => k.IdempotencyKeyId);

        builder.Property(k => k.IdempotencyKeyValue)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(k => k.IdempotencyKeyValue)
            .IsUnique();

        builder.HasIndex(k => new { k.UserId, k.IdempotencyKeyValue })
            .IsUnique()
            .HasName("UQ_UserId_IdempotencyKeyValue");

        builder.HasIndex(k => k.ExpiresAt);
    }
}
```

---

## 6. Extension Methods

```csharp
namespace Application.Extensions;

public static class PaymentExtensions
{
    /// <summary>
    /// Generates a random idempotency key
    /// </summary>
    public static string GenerateIdempotencyKey()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Computes hash of request for idempotency comparison
    /// </summary>
    public static string ComputeRequestHash(this PaymentInitiationRequest request)
    {
        var json = JsonConvert.SerializeObject(request);
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    /// <summary>
    /// Determines if payment can be refunded in current state
    /// </summary>
    public static bool CanBeRefunded(this PaymentTransaction payment)
    {
        return payment.CurrentState == PaymentStateEnum.Completed
            || payment.CurrentState == PaymentStateEnum.Settled;
    }

    /// <summary>
    /// Determines next allowed states
    /// </summary>
    public static List<PaymentStateEnum> GetAllowedNextStates(
        this PaymentStateEnum currentState)
    {
        return currentState switch
        {
            PaymentStateEnum.Pending => new()
            {
                PaymentStateEnum.Authorized,
                PaymentStateEnum.Failed
            },
            PaymentStateEnum.Authorized => new()
            {
                PaymentStateEnum.Processing,
                PaymentStateEnum.RequiresAction,
                PaymentStateEnum.Failed
            },
            // ... additional mappings
            _ => new()
        };
    }
}
```

---

These templates provide the foundation for implementing each component of the payment orchestration system. Each can be adapted and extended based on specific requirements.

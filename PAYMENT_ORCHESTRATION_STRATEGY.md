# Comprehensive Payment Orchestration Strategy - FintechAPI

**Document Date:** February 20, 2026  
**Project:** Secure Online Banking API  
**Status:** Strategic Planning & Implementation Guide

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State Analysis](#current-state-analysis)
3. [Architecture Overview](#architecture-overview)
4. [Implementation Strategy](#implementation-strategy)
5. [Detailed Technical Specifications](#detailed-technical-specifications)
6. [Security Framework](#security-framework)
7. [Implementation Roadmap](#implementation-roadmap)

---

## Executive Summary

This document provides a comprehensive strategy for implementing enterprise-grade payment orchestration capabilities including:

- **Payment Orchestration**: Multi-provider support with unified interface
- **Webhook Processing**: Secure, async event handling with retry logic
- **Idempotency**: Prevention of double charges through database-level guarantees
- **State Machine**: Defined payment lifecycle with state transition validation
- **Reconciliation**: Automated daily transaction matching and reporting
- **Security**: Signature verification, secret rotation, and comprehensive audit logging

---

## Current State Analysis

### Existing Infrastructure

**Strengths:**

- ✅ Clean layered architecture (Web → Application → Core → Infrastructure)
- ✅ Repository and Unit of Work pattern implementation
- ✅ Entity Framework Core with SQL Server
- ✅ JWT Authentication with ASP.NET Identity
- ✅ Stripe integration foundation
- ✅ Email notification system
- ✅ Operation logging (though basic)
- ✅ Rate limiting and CORS configured

**Gaps:**

- ❌ No state machine for payment lifecycle
- ❌ No webhook endpoint infrastructure
- ❌ No idempotency key tracking
- ❌ No payment provider abstraction/interface
- ❌ No audit logging with signature verification
- ❌ No reconciliation mechanism
- ❌ No secret rotation system
- ❌ Only Stripe integration (no multi-provider support)
- ❌ Limited error handling and retry logic
- ❌ No event publishing/messaging system

### Existing Core Entities

```
User
├── BankAccount
│   ├── Cards (1..*)
│   │   └── Payments
│   └── Operations (1..*)
└── LoginDetails, Stocks, Roles
```

**Missing Entities:**

- PaymentProvider
- PaymentState
- IdempotencyKey
- WebhookEvent
- ProviderTransaction
- ReconciliationRecord
- AuditLog
- SecretRotation

---

## Architecture Overview

### Proposed Payment Orchestration Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    API Layer (Web)                               │
│  ┌──────────────────────┐         ┌──────────────────────────┐ │
│  │  PaymentsController  │         │  WebhookController       │ │
│  └──────────────────────┘         └──────────────────────────┘ │
└──────────────────┬────────────────────────────────┬─────────────┘
                   │                                │
┌──────────────────▼────────────────────────────────▼─────────────┐
│                  Application Layer                               │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │         Payment Orchestration Service                     │  │
│  ├──────────────────────────────────────────────────────────┤  │
│  │ • Provider Factory & Selection                            │  │
│  │ • Idempotency Management                                  │  │
│  │ • State Machine Transition                                │  │
│  │ • Failover Logic                                          │  │
│  └──────────────────────────────────────────────────────────┘  │
│  ┌──────────────────┐  ┌──────────────────┐                     │
│  │ Provider Service │  │ Webhook Service  │                     │
│  │ (Abstract)       │  │                  │                     │
│  └────────┬─────────┘  └──────────────────┘                     │
│           │                                                       │
│  ┌────────┴─────────┬──────────────┬──────────────┐              │
│  │ Stripe Adapter   │ Paystack     │ Flutterwave  │              │
│  │                  │ Adapter      │ Adapter      │              │
│  └──────────────────┴──────────────┴──────────────┘              │
└──────────────────────────────────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│              Core Domain Models                                  │
│  • PaymentProvider    • PaymentState      • IdempotencyKey      │
│  • PaymentTransaction • WebhookEvent      • AuditLog            │
│  • ReconciliationRecord • SecretRotation                         │
└──────────────────────────────────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│              Infrastructure Layer                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │        Repositories & Data Access                         │  │
│  │ • PaymentRepository      • IdempotencyKeyRepository      │  │
│  │ • WebhookEventRepository • AuditLogRepository            │  │
│  │ • ReconciliationRepository                               │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │            SQL Server Database                            │  │
│  │  ┌─────────┐ ┌──────────┐ ┌────────┐ ┌──────────┐        │  │
│  │  │Payments │ │Webhooks  │ │Audit   │ │IdempotKey│        │  │
│  │  └─────────┘ └──────────┘ └────────┘ └──────────┘        │  │
│  └──────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│            Cross-Cutting Concerns                                │
│  • Signature Verification    • Secret Management               │
│  • Idempotency Validation    • State Transition Rules          │
│  • Audit Logging             • Reconciliation Engine           │
└──────────────────────────────────────────────────────────────────┘
```

---

## Implementation Strategy

### Phase 1: Core Infrastructure (Weeks 1-3)

#### 1.1 New Entities & Models

**Payment States:**

```
PENDING → AUTHORIZED → PROCESSING → SETTLED → COMPLETED
    ↓
    FAILED → REQUIRES_ACTION → CANCELLED

DECLINED (fail state)
REFUND_INITIATED → REFUND_PROCESSING → REFUNDED
```

**Key Entities to Create:**

1. `PaymentTransaction` - Replaces/extends `Payment`
2. `PaymentProvider` - Provider configuration
3. `PaymentState` - State machine definition
4. `IdempotencyKey` - Idempotency tracking
5. `WebhookEvent` - Event storage
6. `AuditLog` - Security audit trail
7. `ProviderTransaction` - Provider response tracking
8. `ReconciliationRecord` - Daily reconciliation records
9. `SecretRotation` - Secret management

#### 1.2 Database Migrations

Create migrations for:

- Payment states table with state machine definition
- Idempotency keys table (composite unique index: UserId + TransactionId)
- Webhook events table
- Audit logs table with encryption
- Provider transactions table
- Reconciliation tracking
- Secret rotation history

#### 1.3 Core Interfaces

Create foundation interfaces:

```csharp
interface IPaymentProvider
{
    string ProviderName { get; }
    Task<PaymentResponse> InitiatePayment(PaymentRequest request);
    Task<PaymentStatusResponse> GetPaymentStatus(string providerTransactionId);
    Task<RefundResponse> RefundPayment(string providerTransactionId, decimal amount);
    Task<bool> VerifyWebhookSignature(string payload, string signature);
}

interface IPaymentOrchestrationService
{
    Task<PaymentResponse> InitiatePayment(PaymentRequest request, string idempotencyKey);
    Task<PaymentStatusResponse> GetPaymentStatus(string transactionId);
    Task HandleStateTransition(PaymentTransaction transaction, PaymentStateEnum newState);
}

interface IIdempotencyService
{
    Task<IdempotencyResult> CheckIdempotency(string idempotencyKey);
    Task<IdempotencyKey> RegisterIdempotencyKey(string transactionId, string idempotencyKey);
}

interface IWebhookProcessingService
{
    Task<bool> ProcessWebhook(string payload, string signature, string provider);
    Task<WebhookEvent> GetWebhookEvent(string eventId);
    Task RetryFailedWebhooks();
}

interface IReconciliationService
{
    Task<ReconciliationReport> GenerateDailyReconciliation(DateTime date);
    Task<List<MismatchRecord>> DetectMismatches();
}

interface IAuditLogService
{
    Task LogPaymentEvent(string transactionId, string eventType, string details, string userId);
    Task LogSecurityEvent(string eventType, string details);
}
```

### Phase 2: Payment Orchestration (Weeks 4-6)

#### 2.1 Provider Abstraction

Create abstract base class:

```csharp
public abstract class BasePaymentProvider : IPaymentProvider
{
    public string ProviderName { get; protected set; }
    protected string ApiKey { get; set; }
    protected string WebhookSecret { get; set; }
    protected HttpClient _httpClient;

    public abstract Task<PaymentResponse> InitiatePayment(PaymentRequest request);
    public abstract Task<PaymentStatusResponse> GetPaymentStatus(string providerTransactionId);
    public abstract Task<RefundResponse> RefundPayment(string providerTransactionId, decimal amount);
    public abstract Task<bool> VerifyWebhookSignature(string payload, string signature);

    protected virtual string ComputeHmacSignature(string payload, string secret) { }
}
```

Create provider implementations:

- `StripePaymentProvider` - Refactor existing Stripe logic
- `PaystackPaymentProvider` - For African markets
- `FlutterwavePaymentProvider` - For African markets
- Mock provider for testing

#### 2.2 Provider Factory Pattern

```csharp
public class PaymentProviderFactory : IPaymentProviderFactory
{
    public IPaymentProvider GetProvider(EnumPaymentGateway gateway)
    {
        return gateway switch
        {
            EnumPaymentGateway.Stripe => new StripePaymentProvider(),
            EnumPaymentGateway.Paystack => new PaystackPaymentProvider(),
            EnumPaymentGateway.Flutterwave => new FlutterwavePaymentProvider(),
            _ => throw new NotSupportedException()
        };
    }

    public IPaymentProvider GetProviderByProvinceAndCurrency(
        string province, EnumCurrency currency) { }
}
```

#### 2.3 Failover Logic

```csharp
public class PaymentOrchestrationService : IPaymentOrchestrationService
{
    public async Task<PaymentResponse> InitiatePaymentWithFailover(
        PaymentRequest request,
        List<EnumPaymentGateway> providerPriority)
    {
        var lastException = new Exception();

        foreach (var gateway in providerPriority)
        {
            try
            {
                var provider = _providerFactory.GetProvider(gateway);
                var response = await provider.InitiatePayment(request);

                if (response.IsSuccessful)
                {
                    await LogProviderSuccess(gateway, request.TransactionId);
                    return response;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                await LogProviderFailure(gateway, request.TransactionId, ex);
                continue; // Try next provider
            }
        }

        throw new AllPaymentProvidersFailedException(lastException);
    }
}
```

### Phase 3: Idempotency (Weeks 7-8)

#### 3.1 Database Schema

```sql
CREATE TABLE IdempotencyKeys (
    IdempotencyKeyId BIGINT PRIMARY KEY IDENTITY(1,1),
    TransactionId NVARCHAR(128) NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    IdempotencyKeyValue NVARCHAR(256) NOT NULL UNIQUE,
    RequestHash NVARCHAR(MAX) NOT NULL,
    ResponseHash NVARCHAR(MAX),
    Status NVARCHAR(50) NOT NULL DEFAULT 'PENDING', -- PENDING, COMPLETED, FAILED
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL, -- Auto-cleanup after 24 hours
    CONSTRAINT UQ_UserId_IdempotencyKeyValue UNIQUE (UserId, IdempotencyKeyValue)
);

CREATE INDEX IDX_ExpiresAt ON IdempotencyKeys(ExpiresAt);
CREATE INDEX IDX_TransactionId ON IdempotencyKeys(TransactionId);
```

#### 3.2 Idempotency Service Implementation

```csharp
public class IdempotencyService : IIdempotencyService
{
    public async Task<IdempotencyResult> CheckIdempotency(
        string idempotencyKey, string userId, string requestHash)
    {
        var key = await _repository.GetByKeyAndUser(idempotencyKey, userId);

        if (key == null)
            return IdempotencyResult.NotFound();

        if (key.Status == IdempotencyStatus.Completed)
            return IdempotencyResult.Cached(key.ResponseData);

        if (key.Status == IdempotencyStatus.Pending)
            return IdempotencyResult.StillProcessing();

        return IdempotencyResult.Failed(key.Error);
    }

    public async Task<IdempotencyKey> RegisterIdempotencyKey(
        string transactionId, string idempotencyKey, string userId, string requestHash)
    {
        var key = new IdempotencyKey
        {
            TransactionId = transactionId,
            IdempotencyKeyValue = idempotencyKey,
            UserId = userId,
            RequestHash = requestHash,
            Status = IdempotencyStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        return await _repository.AddAsync(key);
    }

    public async Task MarkAsCompleted(
        string idempotencyKey, string userId, string responseHash, object responseData)
    {
        var key = await _repository.GetByKeyAndUser(idempotencyKey, userId);
        key.Status = IdempotencyStatus.Completed;
        key.ResponseHash = responseHash;
        key.ResponseData = JsonConvert.SerializeObject(responseData);

        await _repository.UpdateAsync(key);
    }
}
```

### Phase 4: State Machine (Weeks 9-10)

#### 4.1 State Machine Implementation

```csharp
public class PaymentStateTransitionService : IPaymentStateTransitionService
{
    private readonly Dictionary<PaymentStateEnum, HashSet<PaymentStateEnum>>
        _allowedTransitions = new()
    {
        { PaymentStateEnum.Pending,
            new() { PaymentStateEnum.Authorized, PaymentStateEnum.Failed, PaymentStateEnum.Declined } },
        { PaymentStateEnum.Authorized,
            new() { PaymentStateEnum.Processing, PaymentStateEnum.RequiresAction, PaymentStateEnum.Failed } },
        { PaymentStateEnum.Processing,
            new() { PaymentStateEnum.Settled, PaymentStateEnum.Failed } },
        { PaymentStateEnum.Settled,
            new() { PaymentStateEnum.Completed, PaymentStateEnum.RefundInitiated } },
        { PaymentStateEnum.RequiresAction,
            new() { PaymentStateEnum.Processing, PaymentStateEnum.Cancelled, PaymentStateEnum.Failed } },
        // ... other transitions
    };

    public async Task<bool> TransitionState(
        PaymentTransaction payment,
        PaymentStateEnum newState,
        string reason)
    {
        var currentState = payment.CurrentState;

        // Validate transition
        if (!_allowedTransitions.TryGetValue(currentState, out var allowed) ||
            !allowed.Contains(newState))
        {
            throw new InvalidStateTransitionException(
                $"Cannot transition from {currentState} to {newState}");
        }

        // Create audit trail
        var stateHistory = new PaymentStateHistory
        {
            PaymentId = payment.PaymentTransactionId,
            FromState = currentState,
            ToState = newState,
            TransitionedAt = DateTime.UtcNow,
            Reason = reason
        };

        payment.CurrentState = newState;
        payment.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(payment);
        await _stateHistoryRepository.AddAsync(stateHistory);

        return true;
    }
}
```

### Phase 5: Webhook Processing (Weeks 11-13)

#### 5.1 Webhook Infrastructure

```csharp
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookProcessingService _webhookService;
    private readonly ILogger<WebhookController> _logger;

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        var payload = await request.Body.ReadAsStringAsync();
        var signature = request.Headers["Stripe-Signature"].ToString();

        try
        {
            var result = await _webhookService.ProcessWebhook(
                payload, signature, "stripe");

            if (!result)
                return BadRequest("Invalid signature");

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Webhook processing failed: {ex.Message}");
            return StatusCode(500);
        }
    }

    [HttpPost("paystack")]
    public async Task<IActionResult> HandlePaystackWebhook()
    {
        // Similar implementation for Paystack
    }
}
```

#### 5.2 Webhook Processing Service

```csharp
public class WebhookProcessingService : IWebhookProcessingService
{
    public async Task<bool> ProcessWebhook(
        string payload, string signature, string provider)
    {
        // Verify signature first
        var providerConfig = await _configRepository.GetByProvider(provider);
        var provider = _providerFactory.GetProvider(providerConfig.Gateway);

        if (!provider.VerifyWebhookSignature(payload, signature))
        {
            await _auditService.LogSecurityEvent(
                "WebhookSignatureVerificationFailed",
                $"Provider: {provider}, Signature invalid");
            return false;
        }

        // Parse event
        var webhookEvent = ParseWebhookPayload(payload, provider);

        // Store event (for idempotency)
        await _webhookRepository.AddAsync(webhookEvent);

        // Check idempotency
        var existingEvent = await _webhookRepository.GetByProviderEventId(
            webhookEvent.ProviderEventId);

        if (existingEvent?.ProcessedAt != null)
        {
            _logger.LogInformation($"Webhook already processed: {webhookEvent.ProviderEventId}");
            return true; // Idempotent
        }

        // Queue for processing (async)
        await _messageQueue.EnqueueAsync(new WebhookProcessingJob
        {
            WebhookId = webhookEvent.WebhookEventId
        });

        return true;
    }

    public async Task ProcessWebhookAsync(string webhookId)
    {
        var webhookEvent = await _webhookRepository.GetByIdAsync(webhookId);

        try
        {
            // Route event to appropriate handler
            var handler = _webhookHandlerFactory.GetHandler(webhookEvent.EventType);
            await handler.Handle(webhookEvent);

            // Mark as processed
            webhookEvent.ProcessedAt = DateTime.UtcNow;
            webhookEvent.Status = WebhookStatus.Processed;

            await _webhookRepository.UpdateAsync(webhookEvent);
        }
        catch (Exception ex)
        {
            webhookEvent.RetryCount++;
            webhookEvent.LastError = ex.Message;

            if (webhookEvent.RetryCount < 5)
            {
                // Schedule retry (exponential backoff)
                var nextRetryAt = DateTime.UtcNow.AddMinutes(
                    Math.Pow(2, webhookEvent.RetryCount));

                webhookEvent.NextRetryAt = nextRetryAt;
                webhookEvent.Status = WebhookStatus.PendingRetry;
            }
            else
            {
                webhookEvent.Status = WebhookStatus.Failed;
                await _alertService.SendAlert("WebhookMaxRetriesExceeded", webhookEvent);
            }

            await _webhookRepository.UpdateAsync(webhookEvent);
        }
    }

    public async Task RetryFailedWebhooks()
    {
        var failedWebhooks = await _webhookRepository.GetPendingRetries();

        foreach (var webhook in failedWebhooks)
        {
            await _messageQueue.EnqueueAsync(new WebhookProcessingJob
            {
                WebhookId = webhook.WebhookEventId
            });
        }
    }
}
```

#### 5.3 Webhook Event Handlers

```csharp
public interface IWebhookEventHandler
{
    string EventType { get; }
    Task Handle(WebhookEvent webhookEvent);
}

public class PaymentSuccessHandler : IWebhookEventHandler
{
    public string EventType => "payment.success";

    public async Task Handle(WebhookEvent webhookEvent)
    {
        var paymentData = JsonConvert.DeserializeObject<PaymentWebhookData>(
            webhookEvent.Payload);

        var payment = await _paymentRepository.GetByProviderTransactionId(
            paymentData.ProviderTransactionId);

        // Transition state
        await _stateService.TransitionState(
            payment,
            PaymentStateEnum.Settled,
            "Provider webhook: payment.success");

        // Update provider transaction
        payment.ProviderTransactionDetails = webhookEvent.Payload;
        payment.ProviderConfirmedAt = DateTime.UtcNow;

        await _paymentRepository.UpdateAsync(payment);

        // Send notification
        await _notificationService.SendPaymentSuccessNotification(payment);
    }
}
```

### Phase 6: Reconciliation (Weeks 14-15)

#### 6.1 Reconciliation Service

```csharp
public class ReconciliationService : IReconciliationService
{
    public async Task<ReconciliationReport> GenerateDailyReconciliation(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var report = new ReconciliationReport
        {
            ReportDate = date,
            GeneratedAt = DateTime.UtcNow
        };

        // Get all internal transactions for the day
        var internalTransactions = await _paymentRepository
            .GetByDateRange(startOfDay, endOfDay);

        // Get provider transactions for each provider
        var providers = await _providerRepository.GetAllActiveAsync();

        foreach (var provider in providers)
        {
            var providerImpl = _providerFactory.GetProvider(provider.Gateway);

            // Fetch transactions from provider
            var providerTransactions = await providerImpl.GetDailyTransactions(date);

            // Match transactions
            var matches = MatchTransactions(
                internalTransactions.Where(t => t.Provider == provider.Gateway),
                providerTransactions);

            report.ProviderReconciliations.Add(new ProviderReconciliation
            {
                Provider = provider.Name,
                InternalCount = internalTransactions.Count,
                ProviderCount = providerTransactions.Count,
                MatchedCount = matches.Count,
                Mismatches = DetectMismatches(matches)
            });
        }

        // Generate summary
        report.TotalAmount = internalTransactions.Sum(t => t.Amount);
        report.SuccessfulTransactions = internalTransactions
            .Count(t => t.CurrentState == PaymentStateEnum.Completed);
        report.FailedTransactions = internalTransactions
            .Count(t => t.CurrentState == PaymentStateEnum.Failed);

        // Save report
        await _reconciliationRepository.AddAsync(report);

        // Alert if mismatches found
        if (report.HasMismatches)
        {
            await _alertService.SendAlert("ReconciliationMismatches", report);
        }

        return report;
    }

    private List<TransactionMatch> MatchTransactions(
        IEnumerable<PaymentTransaction> internal,
        IEnumerable<ProviderTransaction> provider)
    {
        var matches = new List<TransactionMatch>();
        var internalList = internal.ToList();
        var providerList = provider.ToList();

        foreach (var inTx in internalList)
        {
            var providerMatch = providerList.FirstOrDefault(pt =>
                pt.Amount == inTx.Amount &&
                Math.Abs((pt.TransactionDate - inTx.CreatedAt).TotalMinutes) < 5);

            if (providerMatch != null)
            {
                matches.Add(new TransactionMatch
                {
                    InternalTransaction = inTx,
                    ProviderTransaction = providerMatch,
                    IsMatched = true,
                    MatchedAt = DateTime.UtcNow
                });

                providerList.Remove(providerMatch);
            }
        }

        // Unmatched internal transactions
        var unmatchedInternal = internalList
            .Where(i => !matches.Any(m => m.InternalTransaction.PaymentTransactionId ==
                i.PaymentTransactionId))
            .ToList();

        foreach (var tx in unmatchedInternal)
        {
            matches.Add(new TransactionMatch
            {
                InternalTransaction = tx,
                IsMatched = false,
                MismatchReason = "Not found in provider records"
            });
        }

        // Unmatched provider transactions
        foreach (var tx in providerList)
        {
            matches.Add(new TransactionMatch
            {
                ProviderTransaction = tx,
                IsMatched = false,
                MismatchReason = "Not found in internal records"
            });
        }

        return matches;
    }
}
```

#### 6.2 Reconciliation Scheduled Task

```csharp
public class ReconciliationBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run at 2 AM UTC daily
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1).AddHours(2);
                var delay = nextRun - now;

                await Task.Delay(delay, stoppingToken);

                // Generate reconciliation for yesterday
                var yesterday = DateTime.UtcNow.AddDays(-1);
                await _reconciliationService.GenerateDailyReconciliation(yesterday);

                // Generate report
                await _reportService.GenerateReconciliationReport(yesterday);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Reconciliation failed: {ex.Message}");
            }
        }
    }
}
```

### Phase 7: Security & Audit (Weeks 16-17)

#### 7.1 Audit Logging

```csharp
public class AuditLogService : IAuditLogService
{
    public async Task LogPaymentEvent(
        string transactionId,
        string eventType,
        string details,
        string userId,
        string ipAddress)
    {
        var auditLog = new AuditLog
        {
            TransactionId = transactionId,
            EventType = eventType,
            UserId = userId,
            Details = _encryptionService.Encrypt(details),
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow,
            UserAgent = HttpContext.Current?.Request.UserAgent
        };

        await _auditRepository.AddAsync(auditLog);
    }

    public async Task LogSecurityEvent(
        string eventType,
        string details,
        string severity = "WARNING")
    {
        var securityLog = new SecurityAuditLog
        {
            EventType = eventType,
            Details = _encryptionService.Encrypt(details),
            Severity = severity,
            Timestamp = DateTime.UtcNow
        };

        await _securityAuditRepository.AddAsync(securityLog);

        // Alert on critical events
        if (severity == "CRITICAL")
        {
            await _alertService.SendAlert(eventType, details);
        }
    }
}
```

#### 7.2 Secret Management & Rotation

```csharp
public class SecretRotationService : ISecretRotationService
{
    public async Task RotateProviderSecret(
        int providerId,
        string newSecret,
        string rotationReason)
    {
        var provider = await _providerRepository.GetByIdAsync(providerId);

        // Create rotation record
        var rotation = new SecretRotation
        {
            ProviderId = providerId,
            OldSecretHash = _hashService.Hash(provider.WebhookSecret),
            NewSecretHash = _hashService.Hash(newSecret),
            RotatedAt = DateTime.UtcNow,
            RotationReason = rotationReason,
            RotatedBy = CurrentUser.Id
        };

        // Update secret
        provider.WebhookSecret = _encryptionService.Encrypt(newSecret);
        provider.SecretRotatedAt = DateTime.UtcNow;

        await _providerRepository.UpdateAsync(provider);
        await _rotationRepository.AddAsync(rotation);

        // Log security event
        await _auditService.LogSecurityEvent(
            "SecretRotation",
            $"Provider {provider.Name} secret rotated",
            "INFO");
    }

    public async Task RotateExpiredSecrets()
    {
        var providers = await _providerRepository.GetSecretsDueForRotation();

        foreach (var provider in providers)
        {
            try
            {
                var newSecret = _secretGenerationService.GenerateSecureSecret();
                await RotateProviderSecret(
                    provider.ProviderId,
                    newSecret,
                    "Automatic rotation due to age");
            }
            catch (Exception ex)
            {
                await _auditService.LogSecurityEvent(
                    "SecretRotationFailed",
                    $"Provider {provider.Name}: {ex.Message}",
                    "CRITICAL");
            }
        }
    }
}
```

#### 7.3 Signature Verification Utility

```csharp
public class SignatureVerificationService : ISignatureVerificationService
{
    public bool VerifyHmacSha256(
        string payload,
        string signature,
        string secret)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = BitConverter.ToString(computedHash)
                .Replace("-", "").ToLower();

            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(computedSignature));
        }
    }

    public string ComputeHmacSha256(string payload, string secret)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToString(computedHash)
                .Replace("-", "").ToLower();
        }
    }
}
```

---

## Detailed Technical Specifications

### Database Table Definitions

#### Payment Transaction Table

```sql
CREATE TABLE PaymentTransactions (
    PaymentTransactionId BIGINT PRIMARY KEY IDENTITY(1,1),
    TransactionNumber NVARCHAR(50) NOT NULL UNIQUE,
    UserId UNIQUEIDENTIFIER NOT NULL,
    BankAccountId INT NOT NULL,

    -- Payment Details
    Amount DECIMAL(15, 2) NOT NULL,
    Currency NVARCHAR(3) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,

    -- Provider Information
    Provider NVARCHAR(50) NOT NULL,
    ProviderTransactionId NVARCHAR(256),
    ProviderResponse NVARCHAR(MAX),
    ProviderConfirmedAt DATETIME2 NULL,

    -- Idempotency
    IdempotencyKeyId BIGINT,

    -- State Machine
    CurrentState NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
    PreviousState NVARCHAR(50),

    -- Timestamps and Metadata
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2 NULL,

    -- Audit
    InitiatedIpAddress NVARCHAR(45),
    Description NVARCHAR(500),

    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),
    FOREIGN KEY (BankAccountId) REFERENCES BankAccounts(Id),
    FOREIGN KEY (IdempotencyKeyId) REFERENCES IdempotencyKeys(IdempotencyKeyId),

    INDEX IDX_Provider_CreatedAt (Provider, CreatedAt),
    INDEX IDX_CurrentState (CurrentState),
    INDEX IDX_ProviderTransactionId (ProviderTransactionId)
);

CREATE TABLE PaymentStateHistory (
    StateHistoryId BIGINT PRIMARY KEY IDENTITY(1,1),
    PaymentTransactionId BIGINT NOT NULL,
    FromState NVARCHAR(50) NOT NULL,
    ToState NVARCHAR(50) NOT NULL,
    TransitionedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Reason NVARCHAR(500),
    TransitionedBy UNIQUEIDENTIFIER,

    FOREIGN KEY (PaymentTransactionId) REFERENCES PaymentTransactions(PaymentTransactionId),
    INDEX IDX_TransactionId (PaymentTransactionId)
);
```

#### Idempotency Keys Table

```sql
CREATE TABLE IdempotencyKeys (
    IdempotencyKeyId BIGINT PRIMARY KEY IDENTITY(1,1),
    TransactionId NVARCHAR(128) NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    IdempotencyKeyValue NVARCHAR(256) NOT NULL,
    RequestHash NVARCHAR(64) NOT NULL,
    ResponseHash NVARCHAR(64),
    ResponseData NVARCHAR(MAX),
    Status NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
    Error NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,

    CONSTRAINT UQ_User_IdempotencyKey UNIQUE (UserId, IdempotencyKeyValue),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),
    INDEX IDX_ExpiresAt (ExpiresAt),
    INDEX IDX_TransactionId (TransactionId)
);
```

#### Webhook Events Table

```sql
CREATE TABLE WebhookEvents (
    WebhookEventId BIGINT PRIMARY KEY IDENTITY(1,1),
    Provider NVARCHAR(50) NOT NULL,
    ProviderEventId NVARCHAR(256) NOT NULL UNIQUE,
    EventType NVARCHAR(100) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    Signature NVARCHAR(MAX) NOT NULL,

    Status NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
    RetryCount INT DEFAULT 0,
    LastError NVARCHAR(MAX),
    NextRetryAt DATETIME2,
    ProcessedAt DATETIME2 NULL,

    ReceivedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    INDEX IDX_Provider_CreatedAt (Provider, ReceivedAt),
    INDEX IDX_Status_NextRetryAt (Status, NextRetryAt)
);
```

#### Audit Logs Table

```sql
CREATE TABLE AuditLogs (
    AuditLogId BIGINT PRIMARY KEY IDENTITY(1,1),
    TransactionId NVARCHAR(128),
    EventType NVARCHAR(100) NOT NULL,
    UserId UNIQUEIDENTIFIER,
    Details NVARCHAR(MAX) NOT NULL, -- Encrypted
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(MAX),
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    INDEX IDX_TransactionId (TransactionId),
    INDEX IDX_UserId_Timestamp (UserId, Timestamp)
);

CREATE TABLE SecurityAuditLogs (
    SecurityAuditLogId BIGINT PRIMARY KEY IDENTITY(1,1),
    EventType NVARCHAR(100) NOT NULL,
    Details NVARCHAR(MAX) NOT NULL, -- Encrypted
    Severity NVARCHAR(20) NOT NULL, -- INFO, WARNING, CRITICAL
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    INDEX IDX_Severity_Timestamp (Severity, Timestamp)
);
```

#### Reconciliation Tables

```sql
CREATE TABLE ReconciliationReports (
    ReconciliationReportId BIGINT PRIMARY KEY IDENTITY(1,1),
    ReportDate DATE NOT NULL UNIQUE,
    GeneratedAt DATETIME2 NOT NULL,

    TotalAmount DECIMAL(15, 2),
    SuccessfulTransactions INT,
    FailedTransactions INT,

    INDEX IDX_ReportDate (ReportDate)
);

CREATE TABLE ProviderReconciliations (
    ProviderReconciliationId BIGINT PRIMARY KEY IDENTITY(1,1),
    ReconciliationReportId BIGINT NOT NULL,
    Provider NVARCHAR(50) NOT NULL,
    InternalCount INT,
    ProviderCount INT,
    MatchedCount INT,
    HasMismatches BIT,

    FOREIGN KEY (ReconciliationReportId)
        REFERENCES ReconciliationReports(ReconciliationReportId)
);

CREATE TABLE TransactionMismatches (
    MismatchId BIGINT PRIMARY KEY IDENTITY(1,1),
    ProviderReconciliationId BIGINT NOT NULL,
    InternalTransactionId BIGINT,
    ProviderTransactionId NVARCHAR(256),
    MismatchReason NVARCHAR(500),
    Resolution NVARCHAR(500),
    ResolvedAt DATETIME2,

    FOREIGN KEY (ProviderReconciliationId)
        REFERENCES ProviderReconciliations(ProviderReconciliationId)
);
```

---

## Security Framework

### 1. Secret Management

**Initial Setup:**

```csharp
// In configuration/secrets management
var stripeSecret = _configuration["Stripe:SecretKey"]; // From Azure Key Vault
var webhookSecret = _configuration["Stripe:WebhookSecret"]; // From Azure Key Vault
```

**Rotation Strategy:**

- Every 90 days automatically
- Immediate rotation on suspected breach
- Old secrets kept active for 30 days grace period
- All rotations tracked with immutable audit log

### 2. Signature Verification

**For Incoming Webhooks:**

```csharp
public bool VerifyStripeSignature(string payload, string signature, string secret)
{
    var expectedSignature = ComputeHmacSha256(payload, secret);

    // Constant-time comparison to prevent timing attacks
    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(signature.ToLower()),
        Encoding.UTF8.GetBytes(expectedSignature));
}
```

**For Outgoing Requests:**

- All requests to payment providers signed with HMAC-SHA256
- Signature includes timestamp to prevent replay attacks
- Signature expires after 5 minutes

### 3. Data Encryption

**At Rest:**

- Sensitive fields (provider secrets, PII) encrypted with AES-256
- Keys stored in Key Vault, never in code
- Database-level encryption enabled

**In Transit:**

- All communication over HTTPS/TLS 1.2+
- Certificate pinning for critical providers
- Mutual TLS for provider APIs when available

### 4. Audit Logging

**What to Log:**

- ✅ All payment state transitions
- ✅ All webhook processing (payload hash, not actual payload)
- ✅ All secret operations
- ✅ All reconciliation mismatches
- ✅ Failed payment attempts
- ✅ Idempotency cache hits
- ✅ Provider API calls and responses (sanitized)

**What NOT to Log:**

- ❌ Full credit card numbers
- ❌ CVV codes
- ❌ API keys or secrets
- ❌ Customer passwords
- ❌ Webhook payloads (only hashes)

### 5. Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("payment_initiation", policy =>
    {
        policy.Window = TimeSpan.FromMinutes(1);
        policy.PermitLimit = 10;
        policy.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("webhook_processing", policy =>
    {
        policy.Window = TimeSpan.FromSeconds(1);
        policy.PermitLimit = 100;
    });
});
```

---

## Implementation Roadmap

### Timeline: 17 Weeks

| Phase | Week  | Component                         | Status   |
| ----- | ----- | --------------------------------- | -------- |
| 1     | 1-3   | Core Infrastructure & Entities    | Planning |
| 2     | 4-6   | Payment Orchestration & Providers | Planning |
| 3     | 7-8   | Idempotency Implementation        | Planning |
| 4     | 9-10  | State Machine                     | Planning |
| 5     | 11-13 | Webhook Processing                | Planning |
| 6     | 14-15 | Reconciliation                    | Planning |
| 7     | 16-17 | Security & Audit Logging          | Planning |

### Risk Mitigation

| Risk                  | Mitigation                                        |
| --------------------- | ------------------------------------------------- |
| Double charges        | Idempotency keys + unique constraints             |
| Payment loss          | Comprehensive audit logging + reconciliation      |
| Webhook failures      | Retry logic + dead letter queue + alerts          |
| Provider downtime     | Failover to secondary providers                   |
| Data breaches         | Encryption + secret rotation + audit logs         |
| State inconsistencies | State machine validation + transaction boundaries |

### Testing Strategy

**Unit Tests:**

- State machine transitions
- Idempotency validation
- Signature verification
- Provider adapter implementations

**Integration Tests:**

- End-to-end payment flow
- Webhook processing
- Reconciliation matching
- Database transactions

**Load Tests:**

- Concurrent payment processing
- Webhook batch processing
- Reconciliation performance

**Security Tests:**

- Secret exposure scanning
- Signature verification bypasses
- Timing attacks on HMAC
- SQL injection in audit logging

---

## Key Decisions & Rationale

1. **Async Webhook Processing**: Prevents webhook timeouts and allows faster response to provider
2. **Database-Level Idempotency**: Ensures atomicity even with distributed system failures
3. **State Machine Over Simple Flags**: Prevents invalid state combinations and business logic errors
4. **Daily Reconciliation**: Catches mismatches early and provides operational visibility
5. **Audit Logs with Encryption**: Maintains security while providing detective controls
6. **Provider Factory Pattern**: Allows easy addition of new payment providers without modifying orchestration logic
7. **Constant-Time Signature Comparison**: Protects against timing attacks

---

## Dependencies & Prerequisites

### External Services

- Azure Key Vault (secret management)
- Application Insights (logging & monitoring)
- Message Queue (RabbitMQ/Azure Service Bus for webhook processing)
- Email Service (already implemented)

### NuGet Packages to Add

- `FluentValidation`
- `MediatR` (for command/query handling)
- `Polly` (resilience & retry policies)
- `Serilog` (structured logging)
- `CorrelationId` (distributed tracing)

### Development Tools

- Entity Framework Core Migrations
- Database Initialization Scripts
- Postman Collections for API testing
- Load Testing Tools (k6 or JMeter)

---

## Success Metrics

1. **Reliability**
   - 99.99% payment success rate for valid transactions
   - Zero double-charge incidents
   - 100% webhook delivery (with retries)

2. **Performance**
   - Payment initiation < 500ms
   - Webhook processing < 2s (async queue)
   - Daily reconciliation < 5 minutes

3. **Security**
   - 100% audit logged transactions
   - Zero exposed secrets
   - All signatures verified

4. **Operational**
   - Automatic detection of 95%+ mismatches
   - Automatic failover for provider downtime
   - Daily reconciliation reports delivered on schedule

---

## Conclusion

This comprehensive strategy provides a enterprise-grade payment orchestration system addressing all critical requirements. Implementation should follow the phased approach to manage complexity and risk. Regular security audits and load testing are essential throughout implementation.

For questions or clarifications, refer to individual phase specifications or contact the development team.

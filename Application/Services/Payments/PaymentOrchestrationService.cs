using System.Text.Json;
using Application.Interfaces;
using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Payments;

public class PaymentOrchestrationService
{
    private readonly ApplicationDbContext _db;
    private readonly PaymentProviderFactory _providerFactory;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PaymentOrchestrationService> _logger;
    private readonly IConfiguration _configuration;

    public PaymentOrchestrationService(
        ApplicationDbContext db,
        PaymentProviderFactory providerFactory,
        IIdempotencyService idempotencyService,
        IAuditLogService auditLogService,
        ILogger<PaymentOrchestrationService> logger,
        IConfiguration configuration)
    {
        _db = db;
        _providerFactory = providerFactory;
        _idempotencyService = idempotencyService;
        _auditLogService = auditLogService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<(bool success, PaymentTransaction? transaction, string message)> InitiatePaymentWithFailoverAsync(
        Guid userId,
        int bankAccountId,
        decimal amount,
        string currency,
        string customerEmail,
        string? idempotencyKey = null,
        EnumPaymentGateway? preferredProvider = null)
    {
        idempotencyKey ??= Guid.NewGuid().ToString("N");

        try
        {
            // Check idempotency
            var existingKey = await _idempotencyService.GetByKeyAsync(userId, idempotencyKey);
            if (existingKey != null)
            {
                _logger.LogInformation("Duplicate payment request detected for idempotency key {Key}", idempotencyKey);
                
                // Return cached response
                var existingTransaction = await _db.PaymentTransactions
                    .FirstOrDefaultAsync(pt => pt.TransactionNumber == existingKey.TransactionId);
                
                return (true, existingTransaction, "Payment already processed (idempotent)");
            }

            // Create payment transaction
            var transaction = new PaymentTransaction
            {
                TransactionNumber = Guid.NewGuid().ToString("N"),
                UserId = userId,
                BankAccountId = bankAccountId,
                Amount = amount,
                Currency = (EnumCurrency)Enum.Parse(typeof(EnumCurrency), currency),
                Provider = preferredProvider ?? GetDefaultProvider(currency),
                CurrentState = PaymentStateEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.PaymentTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            // Register idempotency key
            await _idempotencyService.RegisterIdempotencyKeyAsync(
                userId,
                transaction.TransactionNumber,
                idempotencyKey,
                ComputeRequestHash(amount, currency, customerEmail),
                TimeSpan.FromHours(24)
            );

            // Log payment initiation
            await _auditLogService.LogPaymentEventAsync(
                "PaymentInitiated",
                transaction.PaymentTransactionId,
                transaction.TransactionNumber,
                userId,
                transaction.Provider.ToString(),
                JsonSerializer.Serialize(new { amount, currency, customerEmail }),
                true
            );

            // Get provider priority list
            var providers = GetProviderPriorityList(currency, preferredProvider);
            
            ProviderPaymentResponse? lastResponse = null;
            Exception? lastException = null;

            // Try each provider with exponential backoff
            foreach (var (providerGateway, attempt) in providers.Select((p, i) => (p, i + 1)))
            {
                try
                {
                    _logger.LogInformation("Attempting payment with provider {Provider} (attempt {Attempt})", 
                        providerGateway, attempt);

                    var provider = _providerFactory.GetProvider(providerGateway);
                    
                    var request = new ProviderPaymentRequest
                    {
                        Amount = amount,
                        Currency = currency,
                        CustomerEmail = customerEmail,
                        IdempotencyKey = idempotencyKey,
                        Metadata = JsonSerializer.Serialize(new
                        {
                            TransactionNumber = transaction.TransactionNumber,
                            UserId = userId,
                            BankAccountId = bankAccountId
                        })
                    };

                    lastResponse = await provider.InitiatePaymentAsync(request);

                    if (lastResponse.IsSuccessful)
                    {
                        // Update transaction with provider details
                        transaction.Provider = providerGateway;
                        transaction.ProviderTransactionId = lastResponse.ProviderTransactionId;
                        transaction.ProviderResponse = lastResponse.RawResponse;
                        transaction.CurrentState = PaymentStateEnum.Authorized;
                        transaction.UpdatedAt = DateTime.UtcNow;

                        await _db.SaveChangesAsync();

                        // Log state transition
                        await _auditLogService.LogStateTransitionAsync(
                            transaction.PaymentTransactionId,
                            PaymentStateEnum.Pending.ToString(),
                            PaymentStateEnum.Authorized.ToString(),
                            $"Payment authorized by {providerGateway}",
                            userId
                        );

                        _logger.LogInformation("Payment successfully initiated with {Provider}", providerGateway);
                        
                        return (true, transaction, "Payment initiated successfully");
                    }

                    _logger.LogWarning("Provider {Provider} failed: {Message}", providerGateway, lastResponse.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error with provider {Provider}", providerGateway);
                    lastException = ex;
                }

                // Exponential backoff before trying next provider
                if (attempt < providers.Count)
                {
                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                    await Task.Delay(delay);
                }
            }

            // All providers failed
            transaction.CurrentState = PaymentStateEnum.Failed;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var errorMessage = lastResponse?.Message ?? lastException?.Message ?? "All payment providers failed";
            
            await _auditLogService.LogPaymentEventAsync(
                "PaymentFailed",
                transaction.PaymentTransactionId,
                transaction.TransactionNumber,
                userId,
                transaction.Provider.ToString(),
                JsonSerializer.Serialize(new { errorMessage, providers = providers.Select(p => p.ToString()) }),
                false
            );

            return (false, transaction, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during payment orchestration");
            
            await _auditLogService.LogPaymentEventAsync(
                "PaymentError",
                null,
                null,
                userId,
                null,
                ex.Message,
                false
            );

            return (false, null, $"Payment processing error: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> RetryFailedPaymentAsync(long paymentTransactionId, Guid userId)
    {
        var transaction = await _db.PaymentTransactions.FindAsync(paymentTransactionId);
        
        if (transaction == null)
            return (false, "Transaction not found");

        if (transaction.UserId != userId)
            return (false, "Unauthorized");

        if (transaction.CurrentState != PaymentStateEnum.Failed)
            return (false, "Only failed payments can be retried");

        // Reset transaction state
        transaction.CurrentState = PaymentStateEnum.Pending;
        transaction.PreviousState = PaymentStateEnum.Failed;
        transaction.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Retry with failover
        var result = await InitiatePaymentWithFailoverAsync(
            transaction.UserId,
            transaction.BankAccountId,
            transaction.Amount,
            transaction.Currency.ToString(),
            "retry@payment.com", // Should get from user
            Guid.NewGuid().ToString("N"), // New idempotency key for retry
            transaction.Provider
        );

        return (result.success, result.message);
    }

    private List<EnumPaymentGateway> GetProviderPriorityList(string currency, EnumPaymentGateway? preferredProvider)
    {
        var providers = new List<EnumPaymentGateway>();

        // Add preferred provider first if specified
        if (preferredProvider.HasValue)
        {
            providers.Add(preferredProvider.Value);
        }

        // Add currency-specific providers
        var currencyProviders = GetProvidersForCurrency(currency);
        providers.AddRange(currencyProviders.Where(p => !providers.Contains(p)));

        // Add fallback providers
        var fallbackProviders = new[] 
        { 
            EnumPaymentGateway.Paystack, 
            EnumPaymentGateway.Flutterwave, 
            EnumPaymentGateway.Remita 
        };
        providers.AddRange(fallbackProviders.Where(p => !providers.Contains(p)));

        return providers;
    }

    private List<EnumPaymentGateway> GetProvidersForCurrency(string currency)
    {
        // Currency-specific provider preferences
        return currency.ToUpper() switch
        {
            "NGN" => new List<EnumPaymentGateway> 
            { 
                EnumPaymentGateway.Paystack, 
                EnumPaymentGateway.Flutterwave, 
                EnumPaymentGateway.Remita 
            },
            "USD" or "EUR" or "GBP" => new List<EnumPaymentGateway> 
            { 
                EnumPaymentGateway.Flutterwave, 
                EnumPaymentGateway.Paystack 
            },
            _ => new List<EnumPaymentGateway> 
            { 
                EnumPaymentGateway.Paystack, 
                EnumPaymentGateway.Flutterwave 
            }
        };
    }

    private EnumPaymentGateway GetDefaultProvider(string currency)
    {
        return currency.ToUpper() switch
        {
            "NGN" => EnumPaymentGateway.Paystack,
            "USD" or "EUR" or "GBP" => EnumPaymentGateway.Flutterwave,
            _ => EnumPaymentGateway.Paystack
        };
    }

    private string ComputeRequestHash(decimal amount, string currency, string email)
    {
        var data = $"{amount}|{currency}|{email}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(bytes);
    }
}

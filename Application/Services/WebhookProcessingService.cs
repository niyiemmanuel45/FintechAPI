using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class WebhookProcessingService : IWebhookProcessingService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WebhookProcessingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, IWebhookHandler> _webhookHandlers;

        public WebhookProcessingService(
            ApplicationDbContext db, 
            ILogger<WebhookProcessingService> logger, 
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _db = db;
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _webhookHandlers = new Dictionary<string, IWebhookHandler>
            {
                { "remita", new RemitaWebhookHandler() },
                { "paystack", new PaystackWebhookHandler() }
            };
        }

        public async Task<bool> ProcessWebhookAsync(string payload, string signature, string provider)
        {

            if (!_webhookHandlers.TryGetValue(provider.ToLower(), out var handler))
            {
                _logger.LogWarning("No webhook handler found for provider {Provider}", provider);
                return false;
            }

            if (!VerifySignature(payload, signature, provider))
            {
                _logger.LogWarning("Invalid webhook signature for provider {Provider}", provider);
                return false;
            }

            var providerEventId = handler.GetEventId(payload);
            if (string.IsNullOrEmpty(providerEventId))
            {
                _logger.LogWarning("Could not extract event ID from webhook payload for provider {Provider}", provider);
                return false;
            }

            var isEventProcessed = await _db.WebhookEvents.AnyAsync(e => e.ProviderEventId == providerEventId && e.Provider == provider);
            if (isEventProcessed)
            {
                _logger.LogWarning("Webhook event {ProviderEventId} from {Provider} has already been processed.", providerEventId, provider);
                return true;
            }

            var webhookEvent = new WebhookEvent
            {
                Provider = provider,
                ProviderEventId = providerEventId,
                EventType = handler.GetEventType(payload),
                Payload = payload,
                Signature = signature,
                Status = "Pending",
                ReceivedAt = DateTime.UtcNow
            };
            _db.WebhookEvents.Add(webhookEvent);
            await _db.SaveChangesAsync();

            _ = Task.Run(() => ProcessEvent(webhookEvent.WebhookEventId));

            return true;
        }

        public bool VerifySignature(string payload, string signature, string provider)
        {
            var secret = _configuration[$"WebhookSecrets:{provider}"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("Webhook secret for provider {Provider} is not configured.", provider);
                return false;
            }

            if (!_webhookHandlers.TryGetValue(provider.ToLower(), out var handler))
            {
                _logger.LogWarning("No webhook handler found for provider {Provider}", provider);
                return false;
            }

            return handler.VerifySignature(payload, signature, secret);
        }

        private async Task ProcessEvent(long webhookEventId)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var webhookEvent = await db.WebhookEvents.FindAsync(webhookEventId);
            if (webhookEvent == null)
            {
                _logger.LogError("Webhook event {WebhookEventId} not found for processing.", webhookEventId);
                return;
            }

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                if (!_webhookHandlers.TryGetValue(webhookEvent.Provider.ToLower(), out var handler))
                {
                    throw new InvalidOperationException($"No webhook handler found for provider {webhookEvent.Provider}");
                }

                var eventDetails = handler.ParseEvent(webhookEvent.Payload);
                if (eventDetails == null)
                {
                    throw new InvalidOperationException("Failed to parse webhook event details.");
                }

                var paymentTransaction = await db.PaymentTransactions
                    .FirstOrDefaultAsync(pt => pt.ProviderTransactionId == eventDetails.ProviderTransactionId);

                if (paymentTransaction == null)
                {
                    throw new InvalidOperationException($"PaymentTransaction with ProviderTransactionId '{eventDetails.ProviderTransactionId}' not found.");
                }

                var previousState = paymentTransaction.CurrentState;
                var newState = GetNewPaymentState(eventDetails.EventType);

                if (newState.HasValue && newState != previousState)
                {
                    paymentTransaction.PreviousState = previousState;
                    paymentTransaction.CurrentState = newState.Value;
                    paymentTransaction.UpdatedAt = DateTime.UtcNow;
                    if (newState == PaymentStateEnum.Completed)
                    {
                        paymentTransaction.CompletedAt = DateTime.UtcNow;
                    }

                    var stateHistory = new PaymentStateHistory
                    {
                        PaymentTransactionId = paymentTransaction.PaymentTransactionId,
                        FromState = previousState,
                        ToState = newState.Value,
                        Reason = $"Webhook event '{eventDetails.EventType}' received from {webhookEvent.Provider}."
                    };
                    db.PaymentStateHistories.Add(stateHistory);
                }

                webhookEvent.Status = "Processed";
                webhookEvent.ProcessedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully processed webhook event {WebhookEventId} for provider {Provider}.", webhookEvent.WebhookEventId, webhookEvent.Provider);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                webhookEvent.Status = "Failed";
                webhookEvent.LastError = ex.Message;
                _logger.LogError(ex, "Error processing webhook event {WebhookEventId}", webhookEventId);

                // Save failure status outside of the transaction
                using var updateScope = _serviceProvider.CreateScope();
                var updateDb = updateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                updateDb.WebhookEvents.Attach(webhookEvent);
                updateDb.Entry(webhookEvent).Property(e => e.Status).IsModified = true;
                updateDb.Entry(webhookEvent).Property(e => e.LastError).IsModified = true;
                await updateDb.SaveChangesAsync();
            }
        }

        private PaymentStateEnum? GetNewPaymentState(string eventType)
        {
            // This mapping can be expanded and made more sophisticated
            return eventType?.ToLower() switch
            {
                // Paystack & others
                "charge.success" => PaymentStateEnum.Completed,
                "charge.failed" => PaymentStateEnum.Failed,

                // Remita
                "payment_successful" => PaymentStateEnum.Completed,
                "payment_failed" => PaymentStateEnum.Failed,
                "insufficient_funds" => PaymentStateEnum.Failed,

                _ => null
            };
        }
    }

    public class WebhookEventDetails
    {
        public string ProviderTransactionId { get; set; }
        public string EventType { get; set; }
    }

    public interface IWebhookHandler
    {
        string GetEventId(string payload);
        string GetEventType(string payload);
        WebhookEventDetails ParseEvent(string payload);
        bool VerifySignature(string payload, string signature, string secret);
    }

    public class RemitaWebhookHandler : IWebhookHandler
    {
        public string GetEventId(string payload) => JsonDocument.Parse(payload).RootElement.GetProperty("RRR").GetString();
        public string GetEventType(string payload) => JsonDocument.Parse(payload).RootElement.GetProperty("status").GetString();

        public WebhookEventDetails ParseEvent(string payload)
        {
            var json = JsonDocument.Parse(payload).RootElement;
            return new WebhookEventDetails
            {
                EventType = json.GetProperty("status").GetString(),
                ProviderTransactionId = json.GetProperty("orderId").GetString()
            };
        }

        public bool VerifySignature(string payload, string signature, string secret)
        {
            // IMPORTANT: This is an assumed implementation based on common Remita patterns.
            // You MUST verify this against the official Remita documentation for your specific integration.
            // The signature is often a SHA512 hash of the orderId + RRR + API_KEY.
            var json = JsonDocument.Parse(payload).RootElement;
            var orderId = json.GetProperty("orderId").GetString();
            var rrr = json.GetProperty("RRR").GetString();

            var hashString = $"{orderId}{rrr}{secret}";

            var expectedSignature = ComputeSha512(hashString);
            return signature == expectedSignature;
        }

        private string ComputeSha512(string data)
        {
            using var sha512 = SHA512.Create();
            var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    public class PaystackWebhookHandler : IWebhookHandler
    {
        public string GetEventId(string payload) => JsonDocument.Parse(payload).RootElement.GetProperty("data").GetProperty("id").GetString();
        public string GetEventType(string payload) => JsonDocument.Parse(payload).RootElement.GetProperty("event").GetString();

        public WebhookEventDetails ParseEvent(string payload)
        {
            var json = JsonDocument.Parse(payload).RootElement;
            return new WebhookEventDetails
            {
                EventType = json.GetProperty("event").GetString(),
                ProviderTransactionId = json.GetProperty("data").GetProperty("reference").GetString()
            };
        }

        public bool VerifySignature(string payload, string signature, string secret)
        {
            var expectedSignature = ComputeHmacSha256(payload, secret);
            return signature == expectedSignature;
        }

        private string ComputeHmacSha256(string data, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
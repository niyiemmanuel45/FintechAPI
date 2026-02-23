using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Payments;

public class PaystackPaymentProvider : BasePaymentProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaystackPaymentProvider> _logger;
    private readonly string _secretKey;
    private readonly string _baseUrl;

    public override string ProviderName => "Paystack";

    public PaystackPaymentProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PaystackPaymentProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _secretKey = _configuration["PaymentProviders:Paystack:SecretKey"] ?? throw new InvalidOperationException("Paystack SecretKey not configured");
        _baseUrl = _configuration["PaymentProviders:Paystack:BaseUrl"] ?? "https://api.paystack.co";
    }

    public override async Task<ProviderPaymentResponse> InitiatePaymentAsync(ProviderPaymentRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                email = request.CustomerEmail,
                amount = (int)(request.Amount * 100), // Paystack expects amount in kobo (smallest currency unit)
                currency = request.Currency.ToUpper(),
                reference = request.IdempotencyKey ?? Guid.NewGuid().ToString("N"),
                metadata = request.Metadata != null ? JsonSerializer.Deserialize<object>(request.Metadata) : null
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_baseUrl}/transaction/initialize", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PaystackInitializeResponse>(responseBody);
                
                return new ProviderPaymentResponse
                {
                    IsSuccessful = result?.status ?? false,
                    ProviderTransactionId = result?.data?.reference,
                    Status = "pending",
                    Message = result?.message ?? "Payment initialized",
                    RawResponse = responseBody
                };
            }

            _logger.LogError("Paystack payment initiation failed: {Response}", responseBody);
            return new ProviderPaymentResponse
            {
                IsSuccessful = false,
                Status = "failed",
                Message = "Payment initiation failed",
                RawResponse = responseBody
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating Paystack payment");
            return new ProviderPaymentResponse
            {
                IsSuccessful = false,
                Status = "error",
                Message = ex.Message
            };
        }
    }

    public override async Task<ProviderPaymentStatus> GetPaymentStatusAsync(string providerTransactionId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
            
            var response = await client.GetAsync($"{_baseUrl}/transaction/verify/{providerTransactionId}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PaystackVerifyResponse>(responseBody);
                
                return new ProviderPaymentStatus
                {
                    ProviderTransactionId = providerTransactionId,
                    Status = result?.data?.status ?? "unknown"
                };
            }

            _logger.LogError("Paystack status check failed: {Response}", responseBody);
            return new ProviderPaymentStatus
            {
                ProviderTransactionId = providerTransactionId,
                Status = "unknown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Paystack payment status");
            return new ProviderPaymentStatus
            {
                ProviderTransactionId = providerTransactionId,
                Status = "error"
            };
        }
    }

    public override async Task<ProviderRefundResponse> RefundPaymentAsync(string providerTransactionId, decimal amount)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

            var payload = new
            {
                transaction = providerTransactionId,
                amount = (int)(amount * 100) // Amount in kobo
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_baseUrl}/refund", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PaystackRefundResponse>(responseBody);
                
                return new ProviderRefundResponse
                {
                    IsSuccessful = result?.status ?? false,
                    Message = result?.message ?? "Refund processed",
                    RawResponse = responseBody
                };
            }

            _logger.LogError("Paystack refund failed: {Response}", responseBody);
            return new ProviderRefundResponse
            {
                IsSuccessful = false,
                Message = "Refund failed",
                RawResponse = responseBody
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Paystack refund");
            return new ProviderRefundResponse
            {
                IsSuccessful = false,
                Message = ex.Message
            };
        }
    }

    public override Task<bool> VerifyWebhookSignatureAsync(string payload, string signature)
    {
        try
        {
            var secret = _configuration["PaymentProviders:Paystack:WebhookSecret"] ?? _secretKey;
            var hash = ComputeHmacSha512(payload, secret);
            
            // Constant-time comparison to prevent timing attacks
            return Task.FromResult(CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hash),
                Encoding.UTF8.GetBytes(signature)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Paystack webhook signature");
            return Task.FromResult(false);
        }
    }

    private string ComputeHmacSha512(string data, string secret)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    // Response DTOs
    private class PaystackInitializeResponse
    {
        public bool status { get; set; }
        public string? message { get; set; }
        public PaystackInitializeData? data { get; set; }
    }

    private class PaystackInitializeData
    {
        public string? authorization_url { get; set; }
        public string? access_code { get; set; }
        public string? reference { get; set; }
    }

    private class PaystackVerifyResponse
    {
        public bool status { get; set; }
        public string? message { get; set; }
        public PaystackTransactionData? data { get; set; }
    }

    private class PaystackTransactionData
    {
        public string? status { get; set; }
        public string? reference { get; set; }
        public decimal amount { get; set; }
    }

    private class PaystackRefundResponse
    {
        public bool status { get; set; }
        public string? message { get; set; }
    }
}

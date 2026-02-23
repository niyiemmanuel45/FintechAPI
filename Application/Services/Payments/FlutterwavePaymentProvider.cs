using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Payments;

public class FlutterwavePaymentProvider : BasePaymentProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FlutterwavePaymentProvider> _logger;
    private readonly string _secretKey;
    private readonly string _baseUrl;

    public override string ProviderName => "Flutterwave";

    public FlutterwavePaymentProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FlutterwavePaymentProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _secretKey = _configuration["PaymentProviders:Flutterwave:SecretKey"] ?? throw new InvalidOperationException("Flutterwave SecretKey not configured");
        _baseUrl = _configuration["PaymentProviders:Flutterwave:BaseUrl"] ?? "https://api.flutterwave.com/v3";
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
                tx_ref = request.IdempotencyKey ?? Guid.NewGuid().ToString("N"),
                amount = request.Amount,
                currency = request.Currency.ToUpper(),
                redirect_url = _configuration["PaymentProviders:Flutterwave:RedirectUrl"] ?? "https://yourapp.com/payment/callback",
                customer = new
                {
                    email = request.CustomerEmail
                },
                customizations = new
                {
                    title = "Payment",
                    description = "Payment transaction"
                },
                meta = request.Metadata != null ? JsonSerializer.Deserialize<object>(request.Metadata) : null
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_baseUrl}/payments", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<FlutterwaveInitializeResponse>(responseBody);
                
                return new ProviderPaymentResponse
                {
                    IsSuccessful = result?.status == "success",
                    ProviderTransactionId = result?.data?.tx_ref,
                    Status = "pending",
                    Message = result?.message ?? "Payment initialized",
                    RawResponse = responseBody
                };
            }

            _logger.LogError("Flutterwave payment initiation failed: {Response}", responseBody);
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
            _logger.LogError(ex, "Error initiating Flutterwave payment");
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
            
            var response = await client.GetAsync($"{_baseUrl}/transactions/verify_by_reference?tx_ref={providerTransactionId}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<FlutterwaveVerifyResponse>(responseBody);
                
                return new ProviderPaymentStatus
                {
                    ProviderTransactionId = providerTransactionId,
                    Status = result?.data?.status ?? "unknown"
                };
            }

            _logger.LogError("Flutterwave status check failed: {Response}", responseBody);
            return new ProviderPaymentStatus
            {
                ProviderTransactionId = providerTransactionId,
                Status = "unknown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Flutterwave payment status");
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

            // First, get the transaction ID from the reference
            var verifyResponse = await client.GetAsync($"{_baseUrl}/transactions/verify_by_reference?tx_ref={providerTransactionId}");
            var verifyBody = await verifyResponse.Content.ReadAsStringAsync();
            var verifyResult = JsonSerializer.Deserialize<FlutterwaveVerifyResponse>(verifyBody);
            
            if (verifyResult?.data?.id == null)
            {
                return new ProviderRefundResponse
                {
                    IsSuccessful = false,
                    Message = "Transaction not found"
                };
            }

            var payload = new
            {
                amount = amount
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_baseUrl}/transactions/{verifyResult.data.id}/refund", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<FlutterwaveRefundResponse>(responseBody);
                
                return new ProviderRefundResponse
                {
                    IsSuccessful = result?.status == "success",
                    Message = result?.message ?? "Refund processed",
                    RawResponse = responseBody
                };
            }

            _logger.LogError("Flutterwave refund failed: {Response}", responseBody);
            return new ProviderRefundResponse
            {
                IsSuccessful = false,
                Message = "Refund failed",
                RawResponse = responseBody
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Flutterwave refund");
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
            var secret = _configuration["PaymentProviders:Flutterwave:WebhookSecret"] ?? _secretKey;
            var hash = ComputeHash(payload, secret);
            
            // Constant-time comparison to prevent timing attacks
            return Task.FromResult(CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hash),
                Encoding.UTF8.GetBytes(signature)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Flutterwave webhook signature");
            return Task.FromResult(false);
        }
    }

    private string ComputeHash(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    // Response DTOs
    private class FlutterwaveInitializeResponse
    {
        public string? status { get; set; }
        public string? message { get; set; }
        public FlutterwaveInitializeData? data { get; set; }
    }

    private class FlutterwaveInitializeData
    {
        public string? link { get; set; }
        public string? tx_ref { get; set; }
    }

    private class FlutterwaveVerifyResponse
    {
        public string? status { get; set; }
        public string? message { get; set; }
        public FlutterwaveTransactionData? data { get; set; }
    }

    private class FlutterwaveTransactionData
    {
        public int id { get; set; }
        public string? status { get; set; }
        public string? tx_ref { get; set; }
        public decimal amount { get; set; }
    }

    private class FlutterwaveRefundResponse
    {
        public string? status { get; set; }
        public string? message { get; set; }
    }
}

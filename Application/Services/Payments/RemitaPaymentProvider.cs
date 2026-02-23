using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Payments;

public class RemitaPaymentProvider : BasePaymentProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RemitaPaymentProvider> _logger;
    private readonly string _merchantId;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public override string ProviderName => "Remita";

    public RemitaPaymentProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RemitaPaymentProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _merchantId = _configuration["PaymentProviders:Remita:MerchantId"] ?? throw new InvalidOperationException("Remita MerchantId not configured");
        _apiKey = _configuration["PaymentProviders:Remita:ApiKey"] ?? throw new InvalidOperationException("Remita ApiKey not configured");
        _baseUrl = _configuration["PaymentProviders:Remita:BaseUrl"] ?? "https://remitademo.net/remita";
    }

    public override async Task<ProviderPaymentResponse> InitiatePaymentAsync(ProviderPaymentRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            
            var orderId = request.IdempotencyKey ?? Guid.NewGuid().ToString("N");
            var rrr = GenerateRRR(); // Remita Retrieval Reference
            
            var payload = new
            {
                serviceTypeId = _configuration["PaymentProviders:Remita:ServiceTypeId"] ?? "4430731",
                amount = request.Amount,
                orderId = orderId,
                payerName = request.CustomerEmail.Split('@')[0],
                payerEmail = request.CustomerEmail,
                payerPhone = "0000000000", // Should be provided in metadata
                description = "Payment transaction"
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var hash = ComputeHash($"{_merchantId}{orderId}{request.Amount}{_apiKey}");

            client.DefaultRequestHeaders.Add("Authorization", $"remitaConsumerKey={_merchantId},remitaConsumerToken={hash}");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_baseUrl}/exapp/api/v1/send/api/echannelsvc/merchant/api/paymentinit", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<RemitaInitializeResponse>(responseBody);
                
                return new ProviderPaymentResponse
                {
                    IsSuccessful = result?.statuscode == "025",
                    ProviderTransactionId = result?.RRR ?? orderId,
                    Status = "pending",
                    Message = result?.status ?? "Payment initialized",
                    RawResponse = responseBody
                };
            }

            _logger.LogError("Remita payment initiation failed: {Response}", responseBody);
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
            _logger.LogError(ex, "Error initiating Remita payment");
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
            
            var hash = ComputeHash($"{providerTransactionId}{_apiKey}{_merchantId}");
            client.DefaultRequestHeaders.Add("Authorization", $"remitaConsumerKey={_merchantId},remitaConsumerToken={hash}");
            
            var response = await client.GetAsync($"{_baseUrl}/exapp/api/v1/send/api/echannelsvc/{_merchantId}/{providerTransactionId}/{hash}/status.reg");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<RemitaStatusResponse>(responseBody);
                
                return new ProviderPaymentStatus
                {
                    ProviderTransactionId = providerTransactionId,
                    Status = MapRemitaStatus(result?.status)
                };
            }

            _logger.LogError("Remita status check failed: {Response}", responseBody);
            return new ProviderPaymentStatus
            {
                ProviderTransactionId = providerTransactionId,
                Status = "unknown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Remita payment status");
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
            // Note: Remita refunds typically require manual processing or specific merchant agreements
            // This is a placeholder implementation
            _logger.LogWarning("Remita refund requested for transaction {TransactionId}. Manual processing may be required.", providerTransactionId);
            
            return await Task.FromResult(new ProviderRefundResponse
            {
                IsSuccessful = false,
                Message = "Remita refunds require manual processing. Please contact support."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Remita refund");
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
            // Remita webhook verification typically uses SHA512 hash
            var json = JsonDocument.Parse(payload).RootElement;
            var orderId = json.GetProperty("orderId").GetString();
            var rrr = json.GetProperty("RRR").GetString();

            var hashString = $"{orderId}{rrr}{_apiKey}";
            var expectedSignature = ComputeSha512(hashString);
            
            // Constant-time comparison to prevent timing attacks
            return Task.FromResult(CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signature)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Remita webhook signature");
            return Task.FromResult(false);
        }
    }

    private string ComputeHash(string data)
    {
        using var sha512 = SHA512.Create();
        var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private string ComputeSha512(string data)
    {
        using var sha512 = SHA512.Create();
        var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private string GenerateRRR()
    {
        // Generate a unique RRR (Remita Retrieval Reference)
        // In production, this would come from Remita API
        return DateTime.UtcNow.Ticks.ToString();
    }

    private string MapRemitaStatus(string? remitaStatus)
    {
        return remitaStatus?.ToLower() switch
        {
            "00" or "01" or "successful" => "success",
            "021" => "pending",
            "026" => "failed",
            _ => "unknown"
        };
    }

    // Response DTOs
    private class RemitaInitializeResponse
    {
        public string? statuscode { get; set; }
        public string? status { get; set; }
        public string? RRR { get; set; }
    }

    private class RemitaStatusResponse
    {
        public string? status { get; set; }
        public string? RRR { get; set; }
        public decimal amount { get; set; }
    }
}

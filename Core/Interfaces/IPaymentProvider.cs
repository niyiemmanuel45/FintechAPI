using Core.DTOs;

namespace Core.Interfaces;

public interface IPaymentProvider
{
    string ProviderName { get; }
    Task<ProviderPaymentResponse> InitiatePaymentAsync(ProviderPaymentRequest request);
    Task<ProviderPaymentStatus> GetPaymentStatusAsync(string providerTransactionId);
    Task<ProviderRefundResponse> RefundPaymentAsync(string providerTransactionId, decimal amount);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature);
}

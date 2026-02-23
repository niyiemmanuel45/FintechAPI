using Core.Enums;
using Core.Interfaces;
using Core.DTOs;

namespace Application.Services.Payments;

public abstract class BasePaymentProvider : IPaymentProvider
{
    public abstract string ProviderName { get; }

    public abstract Task<ProviderPaymentResponse> InitiatePaymentAsync(ProviderPaymentRequest request);

    public abstract Task<ProviderPaymentStatus> GetPaymentStatusAsync(string providerTransactionId);

    public abstract Task<ProviderRefundResponse> RefundPaymentAsync(string providerTransactionId, decimal amount);

    public virtual Task<bool> VerifyWebhookSignatureAsync(string payload, string signature)
    {
        // Default: no-op; providers should override
        return Task.FromResult(true);
    }
}

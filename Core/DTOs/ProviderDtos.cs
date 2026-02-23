namespace Core.DTOs;

public class ProviderPaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string CustomerEmail { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Metadata { get; set; }
}

public class ProviderPaymentResponse
{
    // Keep both names to be compatible with existing code
    public bool Success { get; set; }
    public bool IsSuccessful
    {
        get => Success;
        set => Success = value;
    }

    public string? ProviderTransactionId { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public string? RawResponse { get; set; }
}

public class ProviderPaymentStatus
{
    public string? ProviderTransactionId { get; set; }
    public string? Status { get; set; }
}

public class ProviderRefundResponse
{
    public bool Success { get; set; }
    public bool IsSuccessful
    {
        get => Success;
        set => Success = value;
    }
    public string? Message { get; set; }
    public string? RawResponse { get; set; }
}

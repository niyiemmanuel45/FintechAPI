namespace Application.DTOs.Payments;

public class ProviderPaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string CustomerEmail { get; set; }
    public string IdempotencyKey { get; set; }
    public string? Metadata { get; set; }
}

public class ProviderPaymentResponse
{
    public bool IsSuccessful { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
}

public class ProviderPaymentStatus
{
    public string ProviderTransactionId { get; set; }
    public string Status { get; set; }
}

public class ProviderRefundResponse
{
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
}

namespace Application.Interfaces
{
    public interface IWebhookProcessingService
    {
        Task<bool> ProcessWebhookAsync(string payload, string signature, string provider);
        bool VerifySignature(string payload, string signature, string provider);
    }
}
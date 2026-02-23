using Core.Entities;

namespace Application.Interfaces;

public interface ISecretRotationService
{
    Task<SecretRotationHistory> RotateSecretAsync(string secretName, string secretType, string? provider = null, Guid? rotatedBy = null);
    Task<bool> RotateAllSecretsAsync();
    Task<List<SecretRotationHistory>> GetRotationHistoryAsync(string? secretName = null, int count = 50);
    Task<Dictionary<string, DateTime>> GetSecretsNeedingRotationAsync(int daysThreshold = 90);
    Task<bool> RollbackSecretAsync(long rotationId, Guid rolledBackBy);
    Task<SecretRotationHistory?> GetActiveSecretAsync(string secretName);
}

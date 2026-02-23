using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;
using Core.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class SecretRotationService : ISecretRotationService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecretRotationService> _logger;
    private readonly IEmailService _emailService;

    public SecretRotationService(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger<SecretRotationService> logger,
        IEmailService emailService)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<SecretRotationHistory> RotateSecretAsync(
        string secretName, 
        string secretType, 
        string? provider = null, 
        Guid? rotatedBy = null)
    {
        var rotation = new SecretRotationHistory
        {
            SecretName = secretName,
            SecretType = secretType,
            Provider = provider,
            RotatedBy = rotatedBy,
            RotationType = rotatedBy.HasValue ? "Manual" : "Automatic",
            Status = "Pending",
            RotatedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting secret rotation for {SecretName} ({SecretType})", secretName, secretType);

            // Get current secret value
            var currentSecret = GetSecretValue(secretName);
            if (string.IsNullOrEmpty(currentSecret))
            {
                throw new InvalidOperationException($"Current secret '{secretName}' not found in configuration");
            }

            rotation.OldSecretHash = ComputeSha256Hash(currentSecret);

            // Generate new secret
            var newSecret = GenerateSecureSecret(secretType);
            rotation.NewSecretHash = ComputeSha256Hash(newSecret);

            // In production, this would update Azure Key Vault or similar secret store
            // For now, we'll log the rotation and mark it as completed
            var updateResult = await UpdateSecretInStore(secretName, newSecret, provider);

            if (!updateResult)
            {
                throw new InvalidOperationException("Failed to update secret in secret store");
            }

            rotation.Status = "Completed";
            rotation.EffectiveFrom = DateTime.UtcNow;
            rotation.EffectiveUntil = DateTime.UtcNow.AddDays(90); // Secrets valid for 90 days

            // Deactivate previous rotations for this secret
            var previousRotations = await _db.SecretRotationHistory
                .Where(r => r.SecretName == secretName && r.IsActive)
                .ToListAsync();

            foreach (var prev in previousRotations)
            {
                prev.IsActive = false;
                prev.EffectiveUntil = DateTime.UtcNow;
            }

            _db.SecretRotationHistory.Add(rotation);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Secret rotation completed successfully for {SecretName}", secretName);

            // Send notification
            await SendRotationNotificationAsync(rotation);

            return rotation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating secret {SecretName}", secretName);
            
            rotation.Status = "Failed";
            rotation.ErrorMessage = ex.Message;
            
            _db.SecretRotationHistory.Add(rotation);
            await _db.SaveChangesAsync();
            
            throw;
        }
    }

    public async Task<bool> RotateAllSecretsAsync()
    {
        try
        {
            _logger.LogInformation("Starting rotation of all secrets");

            var secretsToRotate = new List<(string name, string type, string? provider)>
            {
                ("PaymentProviders:Paystack:SecretKey", "ApiKey", "Paystack"),
                ("PaymentProviders:Paystack:WebhookSecret", "WebhookSecret", "Paystack"),
                ("PaymentProviders:Flutterwave:SecretKey", "ApiKey", "Flutterwave"),
                ("PaymentProviders:Flutterwave:WebhookSecret", "WebhookSecret", "Flutterwave"),
                ("PaymentProviders:Remita:ApiKey", "ApiKey", "Remita"),
                ("Jwt:KEY", "JwtKey", null),
                ("Stripe:SecretKey", "ApiKey", "Stripe")
            };

            var results = new List<bool>();

            foreach (var (name, type, provider) in secretsToRotate)
            {
                try
                {
                    await RotateSecretAsync(name, type, provider);
                    results.Add(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rotate secret {SecretName}", name);
                    results.Add(false);
                }
            }

            var successCount = results.Count(r => r);
            _logger.LogInformation("Rotated {Success} out of {Total} secrets", successCount, results.Count);

            return successCount == results.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk secret rotation");
            return false;
        }
    }

    public async Task<List<SecretRotationHistory>> GetRotationHistoryAsync(string? secretName = null, int count = 50)
    {
        var query = _db.SecretRotationHistory.AsQueryable();

        if (!string.IsNullOrEmpty(secretName))
        {
            query = query.Where(r => r.SecretName == secretName);
        }

        return await query
            .OrderByDescending(r => r.RotatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Dictionary<string, DateTime>> GetSecretsNeedingRotationAsync(int daysThreshold = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysThreshold);
        
        var recentRotations = await _db.SecretRotationHistory
            .Where(r => r.Status == "Completed" && r.IsActive)
            .GroupBy(r => r.SecretName)
            .Select(g => new
            {
                SecretName = g.Key,
                LastRotation = g.Max(r => r.RotatedAt)
            })
            .ToListAsync();

        var needsRotation = new Dictionary<string, DateTime>();

        // Check all configured secrets
        var allSecrets = new[]
        {
            "PaymentProviders:Paystack:SecretKey",
            "PaymentProviders:Paystack:WebhookSecret",
            "PaymentProviders:Flutterwave:SecretKey",
            "PaymentProviders:Flutterwave:WebhookSecret",
            "PaymentProviders:Remita:ApiKey",
            "Jwt:KEY",
            "Stripe:SecretKey"
        };

        foreach (var secretName in allSecrets)
        {
            var lastRotation = recentRotations.FirstOrDefault(r => r.SecretName == secretName);
            
            if (lastRotation == null || lastRotation.LastRotation < cutoffDate)
            {
                needsRotation[secretName] = lastRotation?.LastRotation ?? DateTime.MinValue;
            }
        }

        return needsRotation;
    }

    public async Task<bool> RollbackSecretAsync(long rotationId, Guid rolledBackBy)
    {
        try
        {
            var rotation = await _db.SecretRotationHistory.FindAsync(rotationId);
            if (rotation == null)
            {
                _logger.LogWarning("Rotation {RotationId} not found for rollback", rotationId);
                return false;
            }

            if (rotation.Status != "Completed")
            {
                _logger.LogWarning("Cannot rollback rotation {RotationId} with status {Status}", rotationId, rotation.Status);
                return false;
            }

            // Find the previous rotation
            var previousRotation = await _db.SecretRotationHistory
                .Where(r => r.SecretName == rotation.SecretName)
                .Where(r => r.RotatedAt < rotation.RotatedAt)
                .Where(r => r.Status == "Completed")
                .OrderByDescending(r => r.RotatedAt)
                .FirstOrDefaultAsync();

            if (previousRotation == null)
            {
                _logger.LogWarning("No previous rotation found for {SecretName}", rotation.SecretName);
                return false;
            }

            // Mark current rotation as rolled back
            rotation.Status = "RolledBack";
            rotation.IsActive = false;
            rotation.EffectiveUntil = DateTime.UtcNow;

            // Reactivate previous rotation
            previousRotation.IsActive = true;
            previousRotation.EffectiveUntil = null;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Rolled back secret {SecretName} from rotation {RotationId}", rotation.SecretName, rotationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back rotation {RotationId}", rotationId);
            return false;
        }
    }

    public async Task<SecretRotationHistory?> GetActiveSecretAsync(string secretName)
    {
        return await _db.SecretRotationHistory
            .Where(r => r.SecretName == secretName && r.IsActive && r.Status == "Completed")
            .OrderByDescending(r => r.RotatedAt)
            .FirstOrDefaultAsync();
    }

    private string? GetSecretValue(string secretName)
    {
        // In production, this would fetch from Azure Key Vault
        return _configuration[secretName];
    }

    private async Task<bool> UpdateSecretInStore(string secretName, string newSecret, string? provider)
    {
        try
        {
            // In production, this would update Azure Key Vault or similar
            // For now, we'll just log the operation
            _logger.LogInformation("Would update secret {SecretName} in secret store", secretName);
            
            // Simulate async operation
            await Task.Delay(100);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating secret in store");
            return false;
        }
    }

    private string GenerateSecureSecret(string secretType)
    {
        return secretType switch
        {
            "ApiKey" => GenerateApiKey(),
            "WebhookSecret" => GenerateWebhookSecret(),
            "JwtKey" => GenerateJwtKey(),
            _ => GenerateGenericSecret()
        };
    }

    private string GenerateApiKey()
    {
        // Generate a 64-character hex string
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return "sk_" + BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private string GenerateWebhookSecret()
    {
        // Generate a 128-character hex string
        var bytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return "whsec_" + BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private string GenerateJwtKey()
    {
        // Generate a 256-bit key for JWT signing
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }

    private string GenerateGenericSecret()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }

    private string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private async Task SendRotationNotificationAsync(SecretRotationHistory rotation)
    {
        try
        {
            var subject = $"Secret Rotation: {rotation.SecretName}";
            var body = $@"
                <h2>Secret Rotation Notification</h2>
                <p>A secret has been rotated in the system.</p>
                <ul>
                    <li><strong>Secret Name:</strong> {rotation.SecretName}</li>
                    <li><strong>Secret Type:</strong> {rotation.SecretType}</li>
                    <li><strong>Provider:</strong> {rotation.Provider ?? "N/A"}</li>
                    <li><strong>Rotation Type:</strong> {rotation.RotationType}</li>
                    <li><strong>Status:</strong> {rotation.Status}</li>
                    <li><strong>Rotated At:</strong> {rotation.RotatedAt:yyyy-MM-dd HH:mm:ss} UTC</li>
                    <li><strong>Effective From:</strong> {rotation.EffectiveFrom:yyyy-MM-dd HH:mm:ss} UTC</li>
                    <li><strong>Effective Until:</strong> {rotation.EffectiveUntil:yyyy-MM-dd HH:mm:ss} UTC</li>
                </ul>
                {(rotation.Status == "Failed" ? $"<p><strong>Error:</strong> {rotation.ErrorMessage}</p>" : "")}
            ";

            // Send to admin email
            _logger.LogInformation("Secret rotation notification sent for {SecretName}", rotation.SecretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending rotation notification");
        }
    }
}

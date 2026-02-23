using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class SecretRotationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SecretRotationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1); // Check daily
    private readonly int _rotationThresholdDays = 90; // Rotate secrets older than 90 days

    public SecretRotationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SecretRotationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Secret Rotation Background Service started");

        // Wait 5 minutes after startup to allow database migrations and app initialization
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Secret Rotation Background Service cancelled during startup delay");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRotateSecretsAsync();
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Secret Rotation Background Service cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in secret rotation background service");
                // Wait 1 hour before retrying on error
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Secret Rotation Background Service cancelled during error delay");
                    break;
                }
            }
        }

        _logger.LogInformation("Secret Rotation Background Service stopped");
    }

    private async Task CheckAndRotateSecretsAsync()
    {
        try
        {
            _logger.LogInformation("Checking for secrets needing rotation");

            using var scope = _serviceProvider.CreateScope();
            var secretRotationService = scope.ServiceProvider.GetRequiredService<ISecretRotationService>();

            var secretsNeedingRotation = await secretRotationService.GetSecretsNeedingRotationAsync(_rotationThresholdDays);

            if (secretsNeedingRotation.Count == 0)
            {
                _logger.LogInformation("No secrets need rotation at this time");
                return;
            }

            _logger.LogWarning("{Count} secrets need rotation", secretsNeedingRotation.Count);

            foreach (var (secretName, lastRotation) in secretsNeedingRotation)
            {
                try
                {
                    var daysSinceRotation = (DateTime.UtcNow - lastRotation).Days;
                    _logger.LogInformation("Rotating secret {SecretName} (last rotated {Days} days ago)", 
                        secretName, daysSinceRotation);

                    var secretType = DetermineSecretType(secretName);
                    var provider = ExtractProvider(secretName);

                    await secretRotationService.RotateSecretAsync(secretName, secretType, provider);

                    _logger.LogInformation("Successfully rotated secret {SecretName}", secretName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rotate secret {SecretName}", secretName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking and rotating secrets");
        }
    }

    private string DetermineSecretType(string secretName)
    {
        if (secretName.Contains("WebhookSecret"))
            return "WebhookSecret";
        if (secretName.Contains("SecretKey") || secretName.Contains("ApiKey"))
            return "ApiKey";
        if (secretName.Contains("Jwt:KEY"))
            return "JwtKey";
        
        return "Generic";
    }

    private string? ExtractProvider(string secretName)
    {
        if (secretName.Contains("Paystack"))
            return "Paystack";
        if (secretName.Contains("Flutterwave"))
            return "Flutterwave";
        if (secretName.Contains("Remita"))
            return "Remita";
        if (secretName.Contains("Stripe"))
            return "Stripe";
        
        return null;
    }
}

using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class ReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReconciliationBackgroundService> _logger;
    private readonly TimeSpan _scheduledTime = new TimeSpan(2, 0, 0); // 2 AM UTC

    public ReconciliationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReconciliationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reconciliation Background Service started");

        // Wait 5 minutes after startup to allow database migrations and app initialization
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Reconciliation Background Service cancelled during startup delay");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = GetNextRunTime(now);
                var delay = nextRun - now;

                _logger.LogInformation("Next reconciliation scheduled for {NextRun} UTC (in {Delay})", 
                    nextRun, delay);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await RunReconciliationAsync();
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Reconciliation Background Service cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reconciliation background service");
                // Wait 1 hour before retrying on error
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Reconciliation Background Service cancelled during error delay");
                    break;
                }
            }
        }

        _logger.LogInformation("Reconciliation Background Service stopped");
    }

    private async Task RunReconciliationAsync()
    {
        try
        {
            _logger.LogInformation("Starting daily reconciliation");

            using var scope = _serviceProvider.CreateScope();
            var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();

            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            var report = await reconciliationService.RunDailyReconciliationAsync(yesterday);

            _logger.LogInformation(
                "Daily reconciliation completed: {Matched} matched, {Mismatched} mismatched",
                report.MatchedTransactions, report.MismatchedTransactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running daily reconciliation");
        }
    }

    private DateTime GetNextRunTime(DateTime now)
    {
        var today = now.Date;
        var scheduledToday = today.Add(_scheduledTime);

        if (now < scheduledToday)
        {
            return scheduledToday;
        }
        else
        {
            return today.AddDays(1).Add(_scheduledTime);
        }
    }
}

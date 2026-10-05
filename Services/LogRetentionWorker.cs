using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;

namespace UplivaAI.Services;

/// <summary>Small, database-only retention job to keep the cost of SQL logging bounded.</summary>
public sealed class LogRetentionWorker(IServiceScopeFactory scopeFactory, ILogger<LogRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<UplivaDbContext>();
                var now = DateTime.UtcNow;
                var logCutoff = now.AddDays(-60);
                var errorCutoff = now.AddDays(-90);

                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM IntegrationLogs WHERE CreatedAtUtc < {logCutoff}", stoppingToken);
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM NotificationLogs WHERE CreatedAtUtc < {logCutoff}", stoppingToken);
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ErrorLogs WHERE CreatedAtUtc < {errorCutoff}", stoppingToken);

                logger.LogInformation("Log retention completed. Integration/notification cutoff={LogCutoff}, error cutoff={ErrorCutoff}", logCutoff, errorCutoff);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Log retention job failed.");
            }

            try { await Task.Delay(TimeSpan.FromHours(24), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }
    }
}

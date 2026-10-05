using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public sealed class IntegrationLogService(UplivaDbContext db, ILogger<IntegrationLogService> logger) : IIntegrationLogService
{
    public async Task WriteAsync(int? businessId, string integrationName, string operation, string status,
        string correlationId, long durationMs, int attempt = 1, int? httpStatusCode = null,
        string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        try
        {
            db.IntegrationLogs.Add(new IntegrationLog
            {
                BusinessId = businessId,
                IntegrationName = Trim(integrationName, 50),
                Operation = Trim(operation, 100),
                Status = Trim(status, 40),
                CorrelationId = Trim(correlationId, 100),
                DurationMs = durationMs,
                Attempt = attempt,
                HttpStatusCode = httpStatusCode,
                ErrorMessage = Trim(errorMessage ?? string.Empty, 2000),
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not persist IntegrationLog. Integration={Integration}, Operation={Operation}, CorrelationId={CorrelationId}", integrationName, operation, correlationId);
        }
    }

    private static string Trim(string value, int length) => value.Length <= length ? value : value[..length];
}

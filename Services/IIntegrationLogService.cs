namespace UplivaAI.Services;

public interface IIntegrationLogService
{
    Task WriteAsync(int? businessId, string integrationName, string operation, string status, string correlationId,
        long durationMs, int attempt = 1, int? httpStatusCode = null, string? errorMessage = null,
        CancellationToken cancellationToken = default);
}

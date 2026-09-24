namespace UplivaAI.Services;

public interface IAuditLogService
{
    Task WriteAsync(
        string action,
        string entityType,
        string entityId,
        int? businessId = null,
        string? details = null,
        CancellationToken cancellationToken = default);
}

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Middleware;

namespace UplivaAI.Services;

public sealed class AuditLogService(
    UplivaDbContext db,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditLogService> logger) : IAuditLogService
{
    public async Task WriteAsync(
        string action,
        string entityType,
        string entityId,
        int? businessId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            var userIdValue = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdValue, out var userId);

            var audit = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                BusinessId = businessId,
                UserId = userId > 0 ? userId : null,
                UserEmail = user?.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                UserRole = user?.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
                CorrelationId = httpContext?.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
                    ?? httpContext?.TraceIdentifier
                    ?? Guid.NewGuid().ToString("N"),
                Details = NormalizeDetails(details),
                CreatedAtUtc = DateTime.UtcNow
            };

            db.AuditLogs.Add(audit);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit failure must never break the business operation that has already succeeded.
            logger.LogError(ex,
                "Unable to persist audit log. Action={Action}, EntityType={EntityType}, EntityId={EntityId}, BusinessId={BusinessId}",
                action, entityType, entityId, businessId);
        }
    }

    private static string NormalizeDetails(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(details);
            return JsonSerializer.Serialize(document.RootElement);
        }
        catch (JsonException)
        {
            return details.Length <= 4000 ? details : details[..4000];
        }
    }
}

using System.Security.Claims;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public sealed class ErrorLogService(
    UplivaDbContext db,
    ILogger<ErrorLogService> logger) : IErrorLogService
{
    public async Task WriteAsync(
        Exception exception,
        HttpContext context,
        string correlationId,
        int statusCode = 500,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = context.User;
            var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdValue, out var userId);
            var businessIdValue = user.FindFirstValue("BusinessId")
                ?? context.Request.RouteValues["id"]?.ToString()
                ?? context.Request.Query["id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(businessIdValue) && context.Request.HasFormContentType)
            {
                try
                {
                    var form = await context.Request.ReadFormAsync(cancellationToken);
                    businessIdValue = form["BusinessId"].FirstOrDefault();
                }
                catch
                {
                    // The original exception is more important than a failed best-effort lookup.
                }
            }

            _ = int.TryParse(businessIdValue, out var businessId);

            db.ErrorLogs.Add(new ErrorLog
            {
                CorrelationId = correlationId,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Message = Truncate(exception.Message, 2000),
                StackTrace = Truncate(exception.ToString(), 10000),
                HttpMethod = context.Request.Method,
                RequestPath = Truncate(context.Request.Path.Value ?? string.Empty, 500),
                UserEmail = Truncate(user.FindFirstValue(ClaimTypes.Email) ?? string.Empty, 200),
                UserId = userId > 0 ? userId : null,
                BusinessId = businessId > 0 ? businessId : null,
                StatusCode = statusCode,
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception logException)
        {
            // Never replace the original application error with an error-log failure.
            logger.LogError(logException,
                "Unable to persist application error to ErrorLogs. CorrelationId={CorrelationId}",
                correlationId);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}

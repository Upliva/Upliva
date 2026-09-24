using System.Text.Json;
using UplivaAI.Services;

namespace UplivaAI.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request was cancelled by the client. Path={Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
                ?? context.TraceIdentifier;

            logger.LogError(ex,
                "Unhandled application exception. CorrelationId={CorrelationId}, Method={Method}, Path={Path}",
                correlationId, context.Request.Method, context.Request.Path);

            var errorLogService = context.RequestServices.GetRequiredService<IErrorLogService>();
            await errorLogService.WriteAsync(ex, context, correlationId, StatusCodes.Status500InternalServerError);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/webhooks"))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    message = "An unexpected error occurred.",
                    correlationId
                }));
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync($"""
<!DOCTYPE html>
<html lang="en">
<head><meta charset="utf-8" /><title>UplivaAI - Error</title></head>
<body style="font-family:Arial,sans-serif;padding:40px">
<h1>Something went wrong</h1>
<p>Please try again. If the problem continues, share this correlation ID with support:</p>
<code>{System.Net.WebUtility.HtmlEncode(correlationId)}</code>
</body>
</html>
""");
        }
    }
}

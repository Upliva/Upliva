namespace UplivaAI.Services;

public interface IErrorLogService
{
    Task WriteAsync(
        Exception exception,
        HttpContext context,
        string correlationId,
        int statusCode = 500,
        CancellationToken cancellationToken = default);
}

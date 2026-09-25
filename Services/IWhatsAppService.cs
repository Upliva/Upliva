namespace UplivaAI.Services;

public interface IWhatsAppService
{
    Task<bool> SendTextAsync(string phoneNumber, string text, CancellationToken cancellationToken = default);
    Task<bool> SendImageAsync(string phoneNumber, string imageUrl, string caption, CancellationToken cancellationToken = default);
    Task<bool> SendWelcomeMenuAsync(string phoneNumber, string? businessName = null, string? customerName = null, CancellationToken cancellationToken = default);

    Task<bool> SendTextForBusinessAsync(int businessId, string phoneNumber, string text, CancellationToken cancellationToken = default);
    Task<bool> SendImageForBusinessAsync(int businessId, string phoneNumber, string imageUrl, string caption, CancellationToken cancellationToken = default);
    Task<bool> SendWelcomeMenuForBusinessAsync(int businessId, string phoneNumber, string? businessName = null, string? customerName = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendCatalogForBusinessAsync(int businessId, string phoneNumber, string mode = "selected", CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> TestConnectionForBusinessAsync(int businessId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendTestMessageForBusinessAsync(int businessId, string phoneNumber, CancellationToken cancellationToken = default);
}

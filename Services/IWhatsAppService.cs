namespace UplivaAI.Services;

public interface IWhatsAppService
{
    Task<bool> SendTextAsync(string phoneNumber, string text, CancellationToken cancellationToken = default);
    Task<bool> SendImageAsync(string phoneNumber, string imageUrl, string caption, CancellationToken cancellationToken = default);
    Task<bool> SendWelcomeMenuAsync(string phoneNumber, string? businessName = null, string? customerName = null, CancellationToken cancellationToken = default);
}

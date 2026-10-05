using UplivaAI.Models;

namespace UplivaAI.Services;

public interface IGupshupWhatsAppService
{
    Task<(bool Success, string Message, string? MessageId)> SendTemplateAsync(
        int businessId,
        WhatsAppTemplateConfiguration template,
        string destination,
        IReadOnlyList<string> parameters,
        CancellationToken cancellationToken = default);
}

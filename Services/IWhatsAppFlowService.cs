namespace UplivaAI.Services;

public interface IWhatsAppFlowService
{
    Task HandleIncomingMessageAsync(
        string from,
        string? customerName,
        string? messageType,
        string? text,
        string? selectionId,
        string? phoneNumberId,
        CancellationToken cancellationToken = default);
}

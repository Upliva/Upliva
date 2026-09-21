namespace UplivaResortBooking.Services;

public interface IWhatsAppFlowService
{
    Task HandleIncomingMessageAsync(
        string from,
        string? customerName,
        string? messageType,
        string? text,
        string? selectionId,
        CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;

namespace UplivaResortBooking.Services;

public class WhatsAppFlowService(
    IWhatsAppService whatsapp,
    IBookingService bookingService,
    ResortDbContext db,
    IConfiguration configuration,
    ILogger<WhatsAppFlowService> logger) : IWhatsAppFlowService
{
    public async Task HandleIncomingMessageAsync(
        string from,
        string? customerName,
        string? messageType,
        string? text,
        string? selectionId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "WhatsApp message received. From: {From}, Type: {MessageType}, Text: {Text}, SelectionId: {SelectionId}",
            from, messageType, text, selectionId);

        if (!string.IsNullOrWhiteSpace(selectionId))
        {
            await HandleSelectionAsync(from, selectionId, cancellationToken);
            return;
        }

        var normalized = text?.Trim().ToLowerInvariant() ?? string.Empty;

        if (normalized is "hi" or "hello" or "start" or "menu" or "hey")
        {
            await whatsapp.SendWelcomeMenuAsync(from, customerName, cancellationToken);
            return;
        }

        await whatsapp.SendWelcomeMenuAsync(from, customerName, cancellationToken);
    }

    private async Task HandleSelectionAsync(
        string from,
        string selectionId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing WhatsApp selection. From: {From}, SelectionId: {SelectionId}",
            from, selectionId);

        switch (selectionId)
        {
            case "BOOK_ROOM":
            case "CHECK_AVAILABILITY":
                await whatsapp.SendBookingMenuAsync(from, cancellationToken);
                break;

            case "BOOKING_WEBSITE":
                var websiteMessage =
                    $"🌴 You can continue your booking here:\n\n" +
                    $"{(configuration["Platform:PublicBaseUrl"] ?? "https://localhost:7248").TrimEnd('/')}/Booking\n\n" +
                    $"If you need help, reply *HELP*.";
                await whatsapp.SendTextAsync(from, websiteMessage, cancellationToken);
                break;

            case "VIEW_PACKAGES":
                var packages = await db.Packages
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Price)
                    .ToListAsync(cancellationToken);

                var packageText = "💰 *Paradise Palm Packages*\n\n" +
                    string.Join("\n\n", packages.Select(p =>
                        $"*{p.Name}*\n{p.Description}\n₹{p.Price:N0} · {p.DurationNights} nights"));

                await whatsapp.SendTextAsync(from, packageText, cancellationToken);
                break;

            case "LOCATION":
                await whatsapp.SendTextAsync(
                    from,
                    "📍 *Paradise Palm Resort*\nBeach Road, Goa, India\n\nGoogle Maps: https://maps.google.com/",
                    cancellationToken);
                break;

            case "CONTACT":
                await whatsapp.SendTextAsync(
                    from,
                    "📞 *Paradise Palm Resort*\nCall: +91 98765 43210\nEmail: reservations@paradisepalm.in",
                    cancellationToken);
                break;

            default:
                if (selectionId.StartsWith("ROOM_", StringComparison.OrdinalIgnoreCase))
                {
                    await whatsapp.SendTextAsync(
                        from,
                        "Great choice! 🏨 Please continue through the booking link so we can securely collect your dates and guest details.",
                        cancellationToken);
                }
                else
                {
                    await whatsapp.SendWelcomeMenuAsync(from, cancellationToken: cancellationToken);
                }
                break;
        }
    }
}

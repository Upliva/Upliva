using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public class WhatsAppFlowService(
    IWhatsAppService whatsapp,
    UplivaDbContext db,
    IConfiguration configuration,
    ILogger<WhatsAppFlowService> logger) : IWhatsAppFlowService
{
    public async Task HandleIncomingMessageAsync(
        string from,
        string? customerName,
        string? messageType,
        string? text,
        string? selectionId,
        string? phoneNumberId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "WhatsApp message received. From: {From}, PhoneNumberId: {PhoneNumberId}, Type: {MessageType}, Text: {Text}, SelectionId: {SelectionId}",
            from, phoneNumberId, messageType, text, selectionId);

        var business = await ResolveBusinessAsync(phoneNumberId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(selectionId))
        {
            await HandleSelectionAsync(from, selectionId, business, cancellationToken);
            return;
        }

        await whatsapp.SendWelcomeMenuAsync(
            from,
            business?.Name,
            customerName,
            cancellationToken);
    }

    private async Task<Business?> ResolveBusinessAsync(
        string? phoneNumberId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phoneNumberId))
            return null;

        return await (
            from business in db.Businesses
            join settings in db.BusinessWhatsAppSettings
                on business.Id equals settings.BusinessId
            where settings.PhoneNumberId == phoneNumberId
                  && settings.IsEnabled
                  && business.Status == BusinessStatuses.Approved
                  && business.IsPublished
            select business)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task HandleSelectionAsync(
        string from,
        string selectionId,
        Business? business,
        CancellationToken cancellationToken)
    {
        switch (selectionId)
        {
            case "VIEW_CATALOG":
                await SendCatalogAsync(from, business, cancellationToken);
                break;

            case "VIEW_OFFERS":
                await SendOffersAsync(from, business, cancellationToken);
                break;

            case "WEBSITE":
                if (business is null)
                {
                    await whatsapp.SendTextAsync(from, "The business website is not configured yet.", cancellationToken);
                    break;
                }

                var baseUrl = (configuration["Platform:PublicBaseUrl"] ?? "https://localhost:7248").TrimEnd('/');
                await whatsapp.SendTextAsync(
                    from,
                    $"🌐 *{business.Name}*\n\nVisit the business website:\n{baseUrl}/business/{business.Slug}",
                    cancellationToken);
                break;

            case "LOCATION":
                await whatsapp.SendTextAsync(
                    from,
                    business is null
                        ? "📍 Business location is not configured yet."
                        : $"📍 *{business.Name}*\n{business.Address}\n{business.City}",
                    cancellationToken);
                break;

            case "CONTACT":
                await whatsapp.SendTextAsync(
                    from,
                    business is null
                        ? "📞 Business contact details are not configured yet."
                        : $"📞 *{business.Name}*\nPhone: {business.PhoneNumber}\nWhatsApp: {business.WhatsAppNumber}\nEmail: {business.Email}",
                    cancellationToken);
                break;

            default:
                await whatsapp.SendWelcomeMenuAsync(from, business?.Name, cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task SendCatalogAsync(
        string from,
        Business? business,
        CancellationToken cancellationToken)
    {
        if (business is null)
        {
            await whatsapp.SendTextAsync(from, "The business catalog is not configured yet.", cancellationToken);
            return;
        }

        // Only the products explicitly selected by the business owner are shown in
        // the WhatsApp showcase. The public website remains the place for the full catalog.
        var items = await db.BusinessCatalogItems
            .AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive && x.IsWhatsAppTopPick)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Take(6)
            .ToListAsync(cancellationToken);

        var baseUrl = (configuration["Platform:PublicBaseUrl"] ?? "https://localhost:7248").TrimEnd('/');
        var websiteUrl = $"{baseUrl}/business/{business.Slug}";

        if (items.Count == 0)
        {
            await whatsapp.SendTextAsync(
                from,
                $"🛍️ *{business.Name}*\n\nThe full catalog is available on the website:\n{websiteUrl}",
                cancellationToken);
            return;
        }

        await whatsapp.SendTextAsync(
            from,
            $"🔥 *{business.Name} – Top {items.Count} picks*\n\nHere are the products selected for our WhatsApp showcase:",
            cancellationToken);

        foreach (var item in items)
        {
            var captionParts = new List<string> { $"*{item.Name}*" };

            if (!string.IsNullOrWhiteSpace(item.Brand) || !string.IsNullOrWhiteSpace(item.Model))
                captionParts.Add(string.Join(" · ", new[] { item.Brand, item.Model }.Where(x => !string.IsNullOrWhiteSpace(x))));

            if (!string.IsNullOrWhiteSpace(item.ShortDescription))
                captionParts.Add(item.ShortDescription);
            else if (!string.IsNullOrWhiteSpace(item.Description))
                captionParts.Add(item.Description);

            if (!string.IsNullOrWhiteSpace(item.PriceText))
                captionParts.Add($"Price: {item.PriceText}");

            if (!string.IsNullOrWhiteSpace(item.DiscountText))
                captionParts.Add(item.DiscountText);

            if (!string.IsNullOrWhiteSpace(item.StockStatus))
                captionParts.Add(item.StockStatus);

            var caption = string.Join("\n", captionParts);

            if (!string.IsNullOrWhiteSpace(item.ImageUrl))
            {
                await whatsapp.SendImageAsync(from, item.ImageUrl, caption, cancellationToken);
            }
            else
            {
                await whatsapp.SendTextAsync(from, caption, cancellationToken);
            }
        }

        await whatsapp.SendTextAsync(
            from,
            $"🌐 *See the complete catalog*\n\n{websiteUrl}\n\nBrowse all products and services on the website.",
            cancellationToken);
    }

    private async Task SendOffersAsync(
        string from,
        Business? business,
        CancellationToken cancellationToken)
    {
        if (business is null)
        {
            await whatsapp.SendTextAsync(from, "The business offers are not configured yet.", cancellationToken);
            return;
        }

        var offers = await db.BusinessOffers
            .AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsPublished)
            .OrderByDescending(x => x.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        if (offers.Count == 0)
        {
            await whatsapp.SendTextAsync(from, $"🔥 *{business.Name}*\n\nNo active offers are available right now.", cancellationToken);
            return;
        }

        var text = $"🔥 *{business.Name} Offers*\n\n" + string.Join("\n\n", offers.Select(x =>
            $"*{x.Title}*\n{x.Description}\n{x.DiscountText}"));

        await whatsapp.SendTextAsync(from, text, cancellationToken);
    }
}

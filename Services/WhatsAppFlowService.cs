using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public class WhatsAppFlowService(
    IWhatsAppService whatsapp,
    UplivaDbContext db,
    IBusinessCacheService businessCache,
    ICatalogTemplateService catalogTemplateService,
    ILogger<WhatsAppFlowService> logger) : IWhatsAppFlowService
{
    public async Task HandleIncomingMessageAsync(
        string from, string? customerName, string? messageType, string? text, string? selectionId, string? phoneNumberId, string? externalMessageId,
        CancellationToken cancellationToken = default)
    {
        var business = await ResolveBusinessAsync(phoneNumberId, cancellationToken);
        if (business is null)
        {
            logger.LogWarning("No active business mapped to WhatsApp PhoneNumberId {PhoneNumberId}", phoneNumberId);
            return;
        }

        var isNewMessage = await LogInboundAsync(business.Id, from, customerName, messageType, text, selectionId, externalMessageId, cancellationToken);
        if (!isNewMessage) return;

        if (!string.IsNullOrWhiteSpace(selectionId))
        {
            await HandleSelectionAsync(from, selectionId, business, cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            var normalized = text.Trim();
            if (normalized.Equals("hi", StringComparison.OrdinalIgnoreCase) || normalized.Equals("hello", StringComparison.OrdinalIgnoreCase) || normalized.Equals("menu", StringComparison.OrdinalIgnoreCase))
            {
                await whatsapp.SendWelcomeMenuForBusinessAsync(business.Id, from, business.Name, customerName, cancellationToken);
                return;
            }

            await CreateEnquiryAsync(business, from, customerName, normalized, cancellationToken);
            await whatsapp.SendTextForBusinessAsync(business.Id, from,
                $"Thanks {customerName ?? "for your message"}! 🙏\n\nWe received your enquiry for *{business.Name}*. Our team will contact you shortly.\n\nReply *Hi* anytime to open the menu.", cancellationToken);
            return;
        }

        await whatsapp.SendWelcomeMenuForBusinessAsync(business.Id, from, business.Name, customerName, cancellationToken);
    }

    private async Task<Business?> ResolveBusinessAsync(string? phoneNumberId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phoneNumberId)) return null;
        return await (from business in db.Businesses
                      join settings in db.BusinessWhatsAppSettings on business.Id equals settings.BusinessId
                      where settings.PhoneNumberId == phoneNumberId && settings.IsEnabled && business.Status == BusinessStatuses.Approved
                      select business).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
    }

    private async Task HandleSelectionAsync(string from, string selectionId, Business business, CancellationToken cancellationToken)
    {
        switch (selectionId)
        {
            case "VIEW_CATALOG":
                await SendCatalogAsync(from, business, cancellationToken);
                break;
            case "VIEW_OFFERS":
                await SendOffersAsync(from, business, cancellationToken);
                break;
            case "LOCATION":
                await whatsapp.SendTextForBusinessAsync(business.Id, from, $"📍 *{business.Name}*\n{business.Address}\n{business.City}, {business.State}", cancellationToken);
                break;
            case "CONTACT":
                await whatsapp.SendTextForBusinessAsync(business.Id, from, $"📞 *{business.Name}*\nPhone: {business.PhoneNumber}\nWhatsApp: {business.WhatsAppNumber}\nEmail: {business.Email}", cancellationToken);
                break;
            default:
                await whatsapp.SendWelcomeMenuForBusinessAsync(business.Id, from, business.Name, cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task SendCatalogAsync(string from, Business business, CancellationToken cancellationToken)
    {
        var settings = await db.BusinessWhatsAppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.BusinessId == business.Id, cancellationToken);
        var limit = Math.Clamp(settings?.FeaturedProductLimit ?? 6, 1, 50);
        var items = await businessCache.GetWhatsAppTopPicksAsync(business.Id, () => db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive && x.IsWhatsAppTopPick)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Take(limit).ToListAsync(cancellationToken), cancellationToken);

        if (items.Count == 0)
        {
            await whatsapp.SendTextForBusinessAsync(business.Id, from, $"🛍️ *{business.Name}*\n\nNo products have been selected for the WhatsApp catalog yet.", cancellationToken);
            return;
        }

        await whatsapp.SendTextForBusinessAsync(business.Id, from, $"🛍️ *{business.Name} Catalog*\n\nHere are our featured products:", cancellationToken);
        foreach (var item in items)
        {
            var caption = BuildCatalogCaption(item, business.BusinessType);
            if (!string.IsNullOrWhiteSpace(item.ImageUrl))
                await whatsapp.SendImageForBusinessAsync(business.Id, from, item.ImageUrl, caption, cancellationToken);
            else
                await whatsapp.SendTextForBusinessAsync(business.Id, from, caption, cancellationToken);
        }
    }

    private async Task SendOffersAsync(string from, Business business, CancellationToken cancellationToken)
    {
        var offers = await db.BusinessOffers.AsNoTracking().Where(x => x.BusinessId == business.Id && x.IsActive)
            .OrderByDescending(x => x.Id).Take(10).ToListAsync(cancellationToken);
        var text = offers.Count == 0
            ? $"🔥 *{business.Name}*\n\nNo active offers are available right now."
            : $"🔥 *{business.Name} Offers*\n\n" + string.Join("\n\n", offers.Select(x => $"*{x.Title}*\n{x.Description}\n{x.DiscountText}"));
        await whatsapp.SendTextForBusinessAsync(business.Id, from, text, cancellationToken);
    }

    private async Task CreateEnquiryAsync(Business business, string phone, string? customerName, string message, CancellationToken cancellationToken)
    {
        db.BusinessEnquiries.Add(new BusinessEnquiry
        {
            BusinessId = business.Id,
            Name = string.IsNullOrWhiteSpace(customerName) ? "WhatsApp Customer" : customerName.Trim(),
            PhoneNumber = phone,
            Message = message,
            Source = "WhatsApp",
            Status = "New",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> LogInboundAsync(int businessId, string from, string? name, string? type, string? text, string? selectionId, string? externalMessageId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(externalMessageId) && await db.WhatsAppMessageLogs.AsNoTracking().AnyAsync(x => x.BusinessId == businessId && x.ExternalMessageId == externalMessageId, cancellationToken))
            return false;

        db.WhatsAppMessageLogs.Add(new WhatsAppMessageLog
        {
            BusinessId = businessId,
            Direction = WhatsAppMessageDirections.Inbound,
            MessageType = string.IsNullOrWhiteSpace(type) ? "unknown" : type,
            CustomerPhoneNumber = from,
            CustomerName = name ?? string.Empty,
            SelectionId = selectionId ?? string.Empty,
            ExternalMessageId = externalMessageId ?? string.Empty,
            MessageText = text ?? string.Empty,
            DeliveryStatus = "Received"
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private string BuildCatalogCaption(BusinessCatalogItem item, string businessType)
    {
        var lines = new List<string> { $"*{item.Name}*" };
        if (!string.IsNullOrWhiteSpace(item.Category)) lines.Add($"Category: {item.Category}");
        if (!string.IsNullOrWhiteSpace(item.PriceText)) lines.Add($"Price: {item.PriceText}");
        if (!string.IsNullOrWhiteSpace(item.DiscountText)) lines.Add(item.DiscountText);
        if (!string.IsNullOrWhiteSpace(item.ShortDescription)) lines.Add(item.ShortDescription);
        else if (!string.IsNullOrWhiteSpace(item.Description)) lines.Add(item.Description);
        if (!string.IsNullOrWhiteSpace(item.StockStatus)) lines.Add($"Availability: {item.StockStatus}");

        try
        {
            foreach (var field in catalogTemplateService.GetDisplayFields(businessType, item.CustomAttributesJson, true))
                lines.Add($"{field.Label}: {field.Value}");
        }
        catch { /* custom fields are optional for outbound messaging */ }

        return string.Join("\n", lines);
    }
}

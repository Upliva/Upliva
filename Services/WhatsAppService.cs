using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

/// <summary>
/// Meta WhatsApp Cloud API adapter. Business credentials are always resolved
/// from BusinessWhatsAppSettings so one UplivaAI instance can serve many businesses.
/// </summary>
public class WhatsAppService(
    HttpClient httpClient,
    IOptions<WhatsAppSettings> options,
    UplivaDbContext db,
    ICatalogTemplateService catalogTemplateService,
    ILogger<WhatsAppService> logger,
    IConfiguration configuration) : IWhatsAppService
{
    private readonly WhatsAppSettings _settings = options.Value;

    public Task<bool> SendTextAsync(string phoneNumber, string text, CancellationToken cancellationToken = default)
    {
        var credentials = ResolvePlatformCredentials();
        return SendAsync(credentials.GraphApiVersion, credentials.PhoneNumberId, credentials.AccessToken, new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "text",
            text = new { preview_url = true, body = text }
        }, cancellationToken);
    }

    public Task<bool> SendImageAsync(string phoneNumber, string imageUrl, string caption, CancellationToken cancellationToken = default)
    {
        var credentials = ResolvePlatformCredentials();
        return SendAsync(credentials.GraphApiVersion, credentials.PhoneNumberId, credentials.AccessToken, new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "image",
            image = new { link = imageUrl, caption }
        }, cancellationToken);
    }

    public Task<bool> SendWelcomeMenuAsync(string phoneNumber, string? businessName = null, string? customerName = null, CancellationToken cancellationToken = default)
    {
        var credentials = ResolvePlatformCredentials();
        return SendWelcomeMenuPayloadAsync(credentials.GraphApiVersion, credentials.PhoneNumberId, credentials.AccessToken, phoneNumber, businessName, customerName, null, cancellationToken);
    }

    public async Task<bool> SendTextForBusinessAsync(int businessId, string phoneNumber, string text, CancellationToken cancellationToken = default)
    {
        var settings = await GetBusinessSettingsAsync(businessId, cancellationToken);
        if (settings is null) return false;

        var success = await SendAsync(settings.GraphApiVersion, settings.PhoneNumberId, settings.AccessToken, new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "text",
            text = new { preview_url = true, body = text }
        }, cancellationToken);

        await LogOutboundAsync(businessId, phoneNumber, "text", text, success, cancellationToken);
        return success;
    }

    public async Task<bool> SendImageForBusinessAsync(int businessId, string phoneNumber, string imageUrl, string caption, CancellationToken cancellationToken = default)
    {
        var settings = await GetBusinessSettingsAsync(businessId, cancellationToken);
        if (settings is null) return false;

        var success = await SendAsync(settings.GraphApiVersion, settings.PhoneNumberId, settings.AccessToken, new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "image",
            image = new { link = imageUrl, caption }
        }, cancellationToken);

        await LogOutboundAsync(businessId, phoneNumber, "image", caption, success, cancellationToken);
        return success;
    }

    public async Task<bool> SendWelcomeMenuForBusinessAsync(int businessId, string phoneNumber, string? businessName = null, string? customerName = null, CancellationToken cancellationToken = default)
    {
        var settings = await GetBusinessSettingsAsync(businessId, cancellationToken);
        if (settings is null) return false;

        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId, cancellationToken);
        var success = await SendWelcomeMenuPayloadAsync(
            settings.GraphApiVersion,
            settings.PhoneNumberId,
            settings.AccessToken,
            phoneNumber,
            string.IsNullOrWhiteSpace(businessName) ? business?.Name : businessName,
            customerName,
            settings.WelcomeMessage,
            cancellationToken);

        await LogOutboundAsync(businessId, phoneNumber, "interactive", settings.WelcomeMessage, success, cancellationToken);
        return success;
    }

    public async Task<(bool Success, string Message)> SendCatalogForBusinessAsync(
        int businessId,
        string phoneNumber,
        string mode = "selected",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return (false, "Enter the recipient WhatsApp number before sending the catalog.");

        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == businessId && x.Status == BusinessStatuses.Approved, cancellationToken);
        if (business is null)
            return (false, "The business is not available or is not approved.");

        var settings = await GetOutboundSettingsForTestingAsync(businessId, cancellationToken);
        if (settings is null)
            return (false, "WhatsApp test configuration was not found. Check Phone Number ID and Access Token.");

        var limit = Math.Clamp(settings.FeaturedProductLimit, 1, 50);
        var query = db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.IsActive);

        var normalizedMode = string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase) ? "all" : "selected";
        List<BusinessCatalogItem> items;

        if (normalizedMode == "all")
        {
            items = await query
                .OrderBy(x => x.Category)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);
        }
        else
        {
            items = await query
                .Where(x => x.IsWhatsAppTopPick)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        if (items.Count == 0)
        {
            return normalizedMode == "all"
                ? (false, "There are no active catalog products to send.")
                : (false, "No WhatsApp products are selected. Select and save at least one product first.");
        }

        var recipient = NormalizePhone(phoneNumber);
        var modeLabel = normalizedMode == "all" ? "all active catalog items" : "selected WhatsApp products";

        // Send a short header first.
        var header = $"🛍️ *{business.Name} Catalog*\n\nHere are our {modeLabel}.";
        var headerResult = await SendDetailedAsync(
            settings.GraphApiVersion,
            settings.PhoneNumberId,
            settings.AccessToken,
            new
            {
                messaging_product = "whatsapp",
                to = recipient,
                type = "text",
                text = new { preview_url = false, body = header }
            },
            cancellationToken);

        await LogOutboundAsync(businessId, recipient, "text", header, headerResult.Success, cancellationToken);

        if (!headerResult.Success)
            return (false, $"Catalog header could not be sent. Meta error: {headerResult.Message}");

        var imageSent = 0;
        var imageFailed = 0;
        var textFallbacks = 0;
        var failures = new List<string>();

        foreach (var item in items)
        {
            var caption = BuildCatalogCaption(item, business.BusinessType);
            // WhatsApp image captions have a practical length limit. Keep the complete
            // product identity and the most useful catalog fields in the caption.
            if (caption.Length > 1000)
                caption = caption[..1000] + "…";

            if (TryGetPublicImageUrl(item.ImageUrl, out var imageUrl))
            {
                // Upload the remote image to Meta first, then send by Media ID. This is
                // more reliable than asking Meta to fetch a third-party URL after accepting
                // the message request.
                var media = await UploadImageFromUrlAsync(
                    settings.GraphApiVersion,
                    settings.PhoneNumberId,
                    settings.AccessToken,
                    imageUrl,
                    cancellationToken);

                if (media.Success && !string.IsNullOrWhiteSpace(media.MediaId))
                {
                    var imageResult = await SendDetailedAsync(
                        settings.GraphApiVersion,
                        settings.PhoneNumberId,
                        settings.AccessToken,
                        new
                        {
                            messaging_product = "whatsapp",
                            to = recipient,
                            type = "image",
                            image = new { id = media.MediaId, caption }
                        },
                        cancellationToken);

                    await LogOutboundAsync(businessId, recipient, "image", caption, imageResult.Success, cancellationToken);

                    if (imageResult.Success)
                    {
                        imageSent++;
                        continue;
                    }

                    imageFailed++;
                    failures.Add($"{item.Name}: image send failed - {imageResult.Message}");
                }
                else
                {
                    imageFailed++;
                    failures.Add($"{item.Name}: image upload failed - {media.Message}");
                }
            }
            else
            {
                imageFailed++;
                failures.Add($"{item.Name}: ImageUrl is empty or is not a public HTTPS URL.");
            }

            // Never lose the catalog details just because an image failed.
            var fallback = caption;
            var fallbackResult = await SendDetailedAsync(
                settings.GraphApiVersion,
                settings.PhoneNumberId,
                settings.AccessToken,
                new
                {
                    messaging_product = "whatsapp",
                    to = recipient,
                    type = "text",
                    text = new { preview_url = false, body = fallback }
                },
                cancellationToken);

            await LogOutboundAsync(businessId, recipient, "text", fallback, fallbackResult.Success, cancellationToken);
            if (fallbackResult.Success)
                textFallbacks++;
            else
                failures.Add($"{item.Name}: text fallback failed - {fallbackResult.Message}");
        }

        if (imageFailed == 0)
            return (true, $"Catalog sent successfully with {imageSent} image(s) and {items.Count} product(s) in rank/order sequence.");

        return (false,
            $"Catalog processing completed: {imageSent} image(s) sent, {imageFailed} image(s) failed, {textFallbacks} text fallback(s) sent. " +
            (failures.Count == 0 ? string.Empty : "Details: " + string.Join(" | ", failures.Take(3))));
    }

    public async Task<(bool Success, string Message)> SendTestMessageForBusinessAsync(int businessId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return (false, "Enter the recipient WhatsApp number.");

        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId && x.Status == BusinessStatuses.Approved, cancellationToken);
        if (business is null) return (false, "The business is not available or is not approved.");

        var settings = await GetOutboundSettingsForTestingAsync(businessId, cancellationToken);
        if (settings is null)
            return (false, "WhatsApp test configuration was not found. Enter/save the Phone Number ID or configure WhatsApp:PhoneNumberId and WhatsApp:AccessToken in User Secrets.");

        var recipient = NormalizePhone(phoneNumber);
        var text = $"UplivaAI WhatsApp test successful ✓\n\nBusiness: {business.Name}\nRecipient: +{recipient}\nTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        var result = await SendDetailedAsync(settings.GraphApiVersion, settings.PhoneNumberId, settings.AccessToken, new
        {
            messaging_product = "whatsapp",
            to = recipient,
            type = "text",
            text = new { preview_url = false, body = text }
        }, cancellationToken);

        await LogOutboundAsync(businessId, recipient, "text", text, result.Success, cancellationToken);
        return (result.Success, result.Message);
    }

    private string BuildCatalogCaption(BusinessCatalogItem item, string businessType)
    {
        var lines = new List<string> { $"*{item.Name}*" };
        if (!string.IsNullOrWhiteSpace(item.Category)) lines.Add($"Category: {item.Category}");
        if (!string.IsNullOrWhiteSpace(item.Brand)) lines.Add($"Brand: {item.Brand}");
        if (!string.IsNullOrWhiteSpace(item.Model)) lines.Add($"Model: {item.Model}");
        if (!string.IsNullOrWhiteSpace(item.SKU)) lines.Add($"SKU: {item.SKU}");
        if (!string.IsNullOrWhiteSpace(item.PriceText)) lines.Add($"Price: {item.PriceText}");
        if (!string.IsNullOrWhiteSpace(item.OriginalPriceText)) lines.Add($"Original price: {item.OriginalPriceText}");
        if (!string.IsNullOrWhiteSpace(item.DiscountText)) lines.Add(item.DiscountText);
        if (!string.IsNullOrWhiteSpace(item.ShortDescription)) lines.Add(item.ShortDescription);
        else if (!string.IsNullOrWhiteSpace(item.Description)) lines.Add(item.Description);
        if (!string.IsNullOrWhiteSpace(item.StockStatus)) lines.Add($"Availability: {item.StockStatus}");
        if (item.Rating.HasValue) lines.Add($"Rating: {item.Rating.Value:0.##}/5");
        if (item.ReviewCount > 0) lines.Add($"Reviews: {item.ReviewCount}");

        try
        {
            foreach (var field in catalogTemplateService.GetDisplayFields(businessType, item.CustomAttributesJson, true))
                lines.Add($"{field.Label}: {field.Value}");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not render custom catalog attributes for ProductId={ProductId}", item.Id);
        }

        return string.Join("\n", lines);
    }

    private (string GraphApiVersion, string PhoneNumberId, string AccessToken) ResolvePlatformCredentials()
    {
        // Supports the exact flat User Secrets format currently used by UplivaAI:
        // "WhatsApp:PhoneNumberId", "WhatsApp:AccessToken", "WhatsApp:GraphApiVersion".
        // Nested appsettings values are also supported because ASP.NET Core exposes
        // both forms through the same configuration key path.
        var graphApiVersion = configuration["WhatsApp:GraphApiVersion"]?.Trim();
        var phoneNumberId = configuration["WhatsApp:PhoneNumberId"]?.Trim();
        var accessToken = configuration["WhatsApp:AccessToken"]?.Trim();

        if (string.IsNullOrWhiteSpace(graphApiVersion))
            graphApiVersion = _settings.GraphApiVersion?.Trim();
        if (string.IsNullOrWhiteSpace(phoneNumberId))
            phoneNumberId = _settings.PhoneNumberId?.Trim();
        if (string.IsNullOrWhiteSpace(accessToken))
            accessToken = _settings.AccessToken?.Trim();

        return (
            string.IsNullOrWhiteSpace(graphApiVersion) ? "v26.0" : graphApiVersion,
            phoneNumberId ?? string.Empty,
            accessToken ?? string.Empty);
    }

    private BusinessWhatsAppSettings ApplyPlatformFallback(BusinessWhatsAppSettings settings)
    {
        // Per-business values win. Missing credentials fall back to the exact
        // WhatsApp:* keys stored in User Secrets/environment configuration.
        var platform = ResolvePlatformCredentials();
        if (string.IsNullOrWhiteSpace(settings.PhoneNumberId))
            settings.PhoneNumberId = platform.PhoneNumberId;
        if (string.IsNullOrWhiteSpace(settings.AccessToken))
            settings.AccessToken = platform.AccessToken;
        if (string.IsNullOrWhiteSpace(settings.WebhookVerifyToken))
            settings.WebhookVerifyToken = configuration["WhatsApp:WebhookVerifyToken"]?.Trim() ?? _settings.WebhookVerifyToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(settings.GraphApiVersion))
            settings.GraphApiVersion = platform.GraphApiVersion;
        return settings;
    }

    public async Task<(bool Success, string Message)> TestConnectionForBusinessAsync(int businessId, CancellationToken cancellationToken = default)
    {
        var settings = await db.BusinessWhatsAppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.BusinessId == businessId, cancellationToken);
        if (settings is null)
        {
            settings = new BusinessWhatsAppSettings { BusinessId = businessId };
        }
        settings = ApplyPlatformFallback(settings);
        if (string.IsNullOrWhiteSpace(settings.PhoneNumberId) || string.IsNullOrWhiteSpace(settings.AccessToken))
            return (false, "Phone Number ID or Access Token was not found. UplivaAI expects WhatsApp:PhoneNumberId and WhatsApp:AccessToken in User Secrets/environment configuration.");

        try
        {
            var version = string.IsNullOrWhiteSpace(settings.GraphApiVersion) ? "v26.0" : settings.GraphApiVersion.Trim();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.facebook.com/{version}/{settings.PhoneNumberId}?fields=id,display_phone_number,verified_name");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
            var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("WhatsApp connection test failed. BusinessId={BusinessId}, Status={Status}, Response={Response}", businessId, (int)response.StatusCode, body);
                return (false, $"WhatsApp connection failed ({(int)response.StatusCode}). {ExtractMetaError(body)}");
            }
            return (true, "WhatsApp connection verified successfully with Meta.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WhatsApp connection test failed. BusinessId={BusinessId}", businessId);
            return (false, "WhatsApp connection could not be tested. Check the application logs.");
        }
    }

    private async Task<BusinessWhatsAppSettings?> GetBusinessSettingsAsync(int businessId, CancellationToken cancellationToken)
    {
        var settings = await db.BusinessWhatsAppSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.IsEnabled, cancellationToken);
        return settings is null ? null : ApplyPlatformFallback(settings);
    }

    private async Task<BusinessWhatsAppSettings?> GetOutboundSettingsForTestingAsync(int businessId, CancellationToken cancellationToken)
    {
        // Deliberately does NOT filter on IsEnabled. This is the outbound test path
        // requested for Meta API testing; webhook/integration enablement remains a
        // separate business setting.
        var settings = await db.BusinessWhatsAppSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == businessId, cancellationToken);

        settings ??= new BusinessWhatsAppSettings { BusinessId = businessId };
        settings = ApplyPlatformFallback(settings);

        if (string.IsNullOrWhiteSpace(settings.PhoneNumberId) || string.IsNullOrWhiteSpace(settings.AccessToken))
            return null;

        return settings;
    }

    private Task<bool> SendWelcomeMenuPayloadAsync(
        string? graphApiVersion,
        string phoneNumberId,
        string accessToken,
        string phoneNumber,
        string? businessName,
        string? customerName,
        string? configuredWelcome,
        CancellationToken cancellationToken)
    {
        var displayBusiness = string.IsNullOrWhiteSpace(businessName) ? "UplivaAI Business" : businessName.Trim();
        var greeting = string.IsNullOrWhiteSpace(customerName) ? "Welcome" : $"Welcome {customerName.Trim()}";
        var body = string.IsNullOrWhiteSpace(configuredWelcome)
            ? $"{greeting}! 👋\n\nHow can we help you today?"
            : configuredWelcome.Trim();

        return SendAsync(graphApiVersion, phoneNumberId, accessToken, new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "interactive",
            interactive = new
            {
                type = "list",
                header = new { type = "text", text = displayBusiness },
                body = new { text = body },
                footer = new { text = "Powered by UplivaAI" },
                action = new
                {
                    button = "Explore",
                    sections = new[]
                    {
                        new
                        {
                            title = "WhatsApp Services",
                            rows = new[]
                            {
                                new { id = "VIEW_CATALOG", title = "🛍️ View Products", description = "Browse products or services" },
                                new { id = "VIEW_OFFERS", title = "🔥 Offers", description = "See active offers" },
                                new { id = "LOCATION", title = "📍 Location", description = "Get business location" },
                                new { id = "CONTACT", title = "📞 Contact", description = "Contact the business" }
                            }
                        }
                    }
                }
            }
        }, cancellationToken);
    }

    private static bool TryGetPublicImageUrl(string? value, out string imageUrl)
    {
        imageUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        imageUrl = uri.ToString();
        return true;
    }

    private async Task<(bool Success, string? MediaId, string Message)> UploadImageFromUrlAsync(
        string? graphApiVersion,
        string phoneNumberId,
        string accessToken,
        string imageUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            using var downloadResponse = await httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!downloadResponse.IsSuccessStatusCode)
                return (false, null, $"Image URL returned HTTP {(int)downloadResponse.StatusCode}.");

            var contentType = downloadResponse.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            if (contentType is not ("image/jpeg" or "image/png"))
                return (false, null, $"Image URL returned unsupported content type '{contentType ?? "unknown"}'. Use JPEG or PNG.");

            var bytes = await downloadResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0)
                return (false, null, "Image URL returned an empty file.");
            if (bytes.Length > 5 * 1024 * 1024)
                return (false, null, "Image is larger than WhatsApp's 5 MB image limit.");

            var version = string.IsNullOrWhiteSpace(graphApiVersion) ? "v26.0" : graphApiVersion.Trim();
            var url = $"https://graph.facebook.com/{version}/{phoneNumberId}/media";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", "catalog-image" + (contentType == "image/png" ? ".png" : ".jpg"));
            form.Add(new StringContent(contentType), "type");
            form.Add(new StringContent("whatsapp"), "messaging_product");
            request.Content = form;

            var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return (false, null, $"HTTP {(int)response.StatusCode}: {ExtractMetaError(body)}");

            using var doc = JsonDocument.Parse(body);
            var mediaId = doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
            if (string.IsNullOrWhiteSpace(mediaId))
                return (false, null, "Meta accepted the upload but did not return a media ID.");

            return (true, mediaId, "Image uploaded to Meta successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload catalog image to Meta. ImageUrl={ImageUrl}", imageUrl);
            return (false, null, $"Image download/upload failed: {ex.Message}");
        }
    }

    private async Task<bool> SendAsync(string? graphApiVersion, string phoneNumberId, string accessToken, object payload, CancellationToken cancellationToken)
    {
        var result = await SendDetailedAsync(graphApiVersion, phoneNumberId, accessToken, payload, cancellationToken);
        return result.Success;
    }

    private sealed record SendResult(bool Success, string Message);

    private async Task<SendResult> SendDetailedAsync(string? graphApiVersion, string phoneNumberId, string accessToken, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogError("WhatsApp configuration is incomplete. PhoneNumberIdPresent={PhonePresent}, AccessTokenPresent={TokenPresent}", !string.IsNullOrWhiteSpace(phoneNumberId), !string.IsNullOrWhiteSpace(accessToken));
            return new(false, "Phone Number ID or Access Token is missing at runtime. Restart the application after updating User Secrets.");
        }

        var version = string.IsNullOrWhiteSpace(graphApiVersion) ? "v26.0" : graphApiVersion.Trim();
        var url = $"https://graph.facebook.com/{version}/{phoneNumberId}/messages";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractMetaError(body);
                logger.LogError("WhatsApp API error. StatusCode={StatusCode}, Response={Response}", (int)response.StatusCode, body);
                return new(false, $"HTTP {(int)response.StatusCode}: {message}");
            }

            logger.LogInformation("WhatsApp message accepted by Meta. StatusCode={StatusCode}, Response={Response}", (int)response.StatusCode, body);
            return new(true, "Meta accepted the message request.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while calling WhatsApp Graph API.");
            return new(false, $"HTTP call failed: {ex.Message}");
        }
    }

    private static string ExtractMetaError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "Meta returned an empty error response.";
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var code = error.TryGetProperty("code", out var c) ? c.ToString() : string.Empty;
                var message = error.TryGetProperty("message", out var m) ? m.GetString() : null;
                var details = error.TryGetProperty("error_data", out var d) && d.TryGetProperty("details", out var detailsValue) ? detailsValue.GetString() : null;
                return string.Join(" | ", new[] { string.IsNullOrWhiteSpace(code) ? null : $"Code {code}", message, details }.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
        }
        catch { }
        return body.Length > 500 ? body[..500] : body;
    }

    private async Task LogOutboundAsync(int businessId, string phoneNumber, string type, string text, bool success, CancellationToken cancellationToken)
    {
        try
        {
            db.WhatsAppMessageLogs.Add(new WhatsAppMessageLog
            {
                BusinessId = businessId,
                Direction = WhatsAppMessageDirections.Outbound,
                MessageType = type,
                CustomerPhoneNumber = NormalizePhone(phoneNumber),
                MessageText = text ?? string.Empty,
                DeliveryStatus = success ? "Submitted" : "Failed"
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist WhatsApp outbound message log. BusinessId={BusinessId}", businessId);
        }
    }

    private static string NormalizePhone(string phoneNumber) => new(phoneNumber.Where(char.IsDigit).ToArray());
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public class WhatsAppService(
    HttpClient httpClient,
    IOptions<WhatsAppSettings> options,
    UplivaDbContext db,
    ILogger<WhatsAppService> logger) : IWhatsAppService
{
    private readonly WhatsAppSettings _settings = options.Value;

    public Task<bool> SendTextAsync(string phoneNumber, string text, CancellationToken cancellationToken = default) =>
        SendAsync(_settings, new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "text",
            text = new { preview_url = true, body = text }
        }, cancellationToken);

    public Task<bool> SendImageAsync(string phoneNumber, string imageUrl, string caption, CancellationToken cancellationToken = default) =>
        SendAsync(_settings, new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "image",
            image = new { link = imageUrl, caption }
        }, cancellationToken);

    public Task<bool> SendWelcomeMenuAsync(string phoneNumber, string? businessName = null, string? customerName = null, CancellationToken cancellationToken = default) =>
        SendWelcomeMenuWithPlatformSettingsAsync(_settings, phoneNumber, businessName, customerName, true, cancellationToken);

    public async Task<bool> SendTextForBusinessAsync(int businessId, string phoneNumber, string text, CancellationToken cancellationToken = default)
    {
        var settings = await GetBusinessSettingsAsync(businessId, cancellationToken);
        if (settings is null) return false;
        return await SendAsync(settings, new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "text",
            text = new { preview_url = true, body = text }
        }, cancellationToken);
    }

    public async Task<bool> SendImageForBusinessAsync(int businessId, string phoneNumber, string imageUrl, string caption, CancellationToken cancellationToken = default)
    {
        var settings = await GetBusinessSettingsAsync(businessId, cancellationToken);
        if (settings is null) return false;
        return await SendAsync(settings, new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "image",
            image = new { link = imageUrl, caption }
        }, cancellationToken);
    }

    public async Task<bool> SendWelcomeMenuForBusinessAsync(int businessId, string phoneNumber, string? businessName = null, string? customerName = null, CancellationToken cancellationToken = default)
    {
        var settings = await GetBusinessSettingsAsync(businessId, cancellationToken);
        if (settings is null) return false;

        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId, cancellationToken);
        var includeWebsite = business is not null && business.ServicePlan != BusinessServicePlans.WhatsAppOnly;
        return await SendWelcomeMenuWithBusinessSettingsAsync(settings, phoneNumber, businessName, customerName, includeWebsite, cancellationToken);
    }

    public async Task<(bool Success, string Message)> TestConnectionForBusinessAsync(int businessId, CancellationToken cancellationToken = default)
    {
        var settings = await GetBusinessSettingsAsync(businessId, cancellationToken);
        if (settings is null)
            return (false, "No WhatsApp configuration exists for this business.");

        if (string.IsNullOrWhiteSpace(settings.PhoneNumberId) || string.IsNullOrWhiteSpace(settings.AccessToken))
            return (false, "Phone Number ID and Access Token are required before testing the connection.");

        try
        {
            var version = string.IsNullOrWhiteSpace(settings.GraphApiVersion) ? "v26.0" : settings.GraphApiVersion.Trim();
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://graph.facebook.com/{version}/{settings.PhoneNumberId}?fields=id,display_phone_number,verified_name");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
            var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("WhatsApp connection test failed. BusinessId={BusinessId}, StatusCode={StatusCode}", businessId, (int)response.StatusCode);
                return (false, $"WhatsApp connection failed ({(int)response.StatusCode}). Check the Phone Number ID and token.");
            }

            logger.LogInformation("WhatsApp connection test succeeded. BusinessId={BusinessId}", businessId);
            return (true, "WhatsApp connection verified successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WhatsApp connection test failed unexpectedly. BusinessId={BusinessId}", businessId);
            return (false, "WhatsApp connection could not be tested. Check the application error logs using the correlation ID from the request.");
        }
    }

    private async Task<BusinessWhatsAppSettings?> GetBusinessSettingsAsync(int businessId, CancellationToken cancellationToken) =>
        await db.BusinessWhatsAppSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.IsEnabled, cancellationToken);

    private Task<bool> SendWelcomeMenuWithPlatformSettingsAsync(
        WhatsAppSettings settings,
        string phoneNumber,
        string? businessName,
        string? customerName,
        bool includeWebsite,
        CancellationToken cancellationToken) =>
        SendWelcomeMenuPayloadAsync(
            settings.GraphApiVersion,
            settings.PhoneNumberId,
            settings.AccessToken,
            phoneNumber,
            businessName,
            customerName,
            true,
            cancellationToken);

    private Task<bool> SendWelcomeMenuWithBusinessSettingsAsync(
        BusinessWhatsAppSettings settings,
        string phoneNumber,
        string? businessName,
        string? customerName,
        bool includeWebsite,
        CancellationToken cancellationToken) =>
        SendWelcomeMenuPayloadAsync(
            settings.GraphApiVersion,
            settings.PhoneNumberId,
            settings.AccessToken,
            phoneNumber,
            businessName,
            customerName,
            includeWebsite,
            cancellationToken);

    private Task<bool> SendWelcomeMenuPayloadAsync(
        string? graphApiVersion,
        string phoneNumberId,
        string accessToken,
        string phoneNumber,
        string? businessName,
        string? customerName,
        bool includeWebsite,
        CancellationToken cancellationToken)
    {
        var displayBusiness = string.IsNullOrWhiteSpace(businessName) ? "UplivaAI Business" : businessName.Trim();
        var greeting = string.IsNullOrWhiteSpace(customerName) ? "Welcome" : $"Welcome {customerName.Trim()}";

        if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogError("WhatsApp welcome menu configuration is incomplete.");
            return Task.FromResult(false);
        }

        return SendRequestAsync(
            graphApiVersion,
            phoneNumberId,
            accessToken,
            new
            {
                messaging_product = "whatsapp",
                to = NormalizePhone(phoneNumber),
                type = "interactive",
                interactive = new
                {
                    type = "list",
                    header = new { type = "text", text = displayBusiness },
                    body = new { text = $"{greeting}! 👋\n\nHow can we help you today?" },
                    footer = new { text = "Powered by UplivaAI" },
                    action = new
                    {
                        button = "Explore",
                        sections = new[]
                        {
                            new
                            {
                                title = "Business Services",
                                rows = BuildBusinessMenuRows(includeWebsite)
                            }
                        }
                    }
                }
            },
            cancellationToken);
    }

    private static object[] BuildBusinessMenuRows(bool includeWebsite)
    {
        var rows = new List<object>
        {
            new { id = "VIEW_CATALOG", title = "🛍️ View Products", description = "Browse products or services" },
            new { id = "LOCATION", title = "📍 Location", description = "Get business location" },
            new { id = "CONTACT", title = "📞 Contact", description = "Contact the business" }
        };

        if (includeWebsite)
            rows.Insert(1, new { id = "WEBSITE", title = "🌐 Visit Website", description = "Open the business website" });

        return rows.ToArray();
    }

    private async Task<bool> SendAsync(WhatsAppSettings settings, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.PhoneNumberId))
        {
            logger.LogError("WhatsApp PhoneNumberId is not configured.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.AccessToken))
        {
            logger.LogError("WhatsApp AccessToken is not configured.");
            return false;
        }

        return await SendRequestAsync(settings.GraphApiVersion, settings.PhoneNumberId, settings.AccessToken, payload, cancellationToken);
    }

    private async Task<bool> SendAsync(BusinessWhatsAppSettings settings, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.PhoneNumberId) || string.IsNullOrWhiteSpace(settings.AccessToken))
        {
            logger.LogError("Business WhatsApp configuration is incomplete. BusinessId={BusinessId}", settings.BusinessId);
            return false;
        }

        return await SendRequestAsync(settings.GraphApiVersion, settings.PhoneNumberId, settings.AccessToken, payload, cancellationToken);
    }

    private async Task<bool> SendRequestAsync(string? graphApiVersion, string phoneNumberId, string accessToken, object payload, CancellationToken cancellationToken)
    {
        var version = string.IsNullOrWhiteSpace(graphApiVersion) ? "v26.0" : graphApiVersion.Trim();
        var url = $"https://graph.facebook.com/{version}/{phoneNumberId}/messages";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("WhatsApp API error. StatusCode={StatusCode}, Response={Response}", (int)response.StatusCode, responseBody);
                return false;
            }

            logger.LogInformation("WhatsApp message sent successfully. StatusCode={StatusCode}", (int)response.StatusCode);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while calling WhatsApp Graph API.");
            return false;
        }
    }

    private static string NormalizePhone(string phoneNumber) =>
        new(phoneNumber.Where(char.IsDigit).ToArray());
}

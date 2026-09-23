using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UplivaAI.Models;

namespace UplivaAI.Services;

public class WhatsAppService(
    HttpClient httpClient,
    IOptions<WhatsAppSettings> options,
    ILogger<WhatsAppService> logger) : IWhatsAppService
{
    private readonly WhatsAppSettings _settings = options.Value;

    public Task<bool> SendTextAsync(string phoneNumber, string text, CancellationToken cancellationToken = default)
    {
        return SendAsync(new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "text",
            text = new { preview_url = true, body = text }
        }, cancellationToken);
    }

    public Task<bool> SendImageAsync(
        string phoneNumber,
        string imageUrl,
        string caption,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "image",
            image = new { link = imageUrl, caption }
        }, cancellationToken);
    }

    public Task<bool> SendWelcomeMenuAsync(
        string phoneNumber,
        string? businessName = null,
        string? customerName = null,
        CancellationToken cancellationToken = default)
    {
        var displayBusiness = string.IsNullOrWhiteSpace(businessName) ? "UplivaAI Business" : businessName.Trim();
        var greeting = string.IsNullOrWhiteSpace(customerName) ? "Welcome" : $"Welcome {customerName.Trim()}";

        return SendAsync(new
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
                            rows = new[]
                            {
                                new { id = "VIEW_CATALOG", title = "🛍️ View Products", description = "Browse products or services" },
                                new { id = "VIEW_OFFERS", title = "🔥 View Offers", description = "See current offers" },
                                new { id = "WEBSITE", title = "🌐 Visit Website", description = "Open the business website" },
                                new { id = "LOCATION", title = "📍 Location", description = "Get business location" },
                                new { id = "CONTACT", title = "📞 Contact", description = "Contact the business" }
                            }
                        }
                    }
                }
            }
        }, cancellationToken);
    }

    private async Task<bool> SendAsync(object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
        {
            logger.LogError("WhatsApp PhoneNumberId is not configured.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.AccessToken))
        {
            logger.LogError("WhatsApp AccessToken is not configured.");
            return false;
        }

        var url = $"https://graph.facebook.com/{_settings.GraphApiVersion}/{_settings.PhoneNumberId}/messages";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "WhatsApp API error. StatusCode: {StatusCode}. Response: {Response}",
                    (int)response.StatusCode,
                    responseBody);
                return false;
            }

            logger.LogInformation("WhatsApp message sent successfully. StatusCode: {StatusCode}", (int)response.StatusCode);
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

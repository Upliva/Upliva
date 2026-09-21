using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UplivaResortBooking.Models;

namespace UplivaResortBooking.Services;

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
            image = new
            {
                link = imageUrl,
                caption
            }
        }, cancellationToken);
    }

    public Task<bool> SendWelcomeMenuAsync(
        string phoneNumber,
        string? customerName = null,
        CancellationToken cancellationToken = default)
    {
        var name = string.IsNullOrWhiteSpace(customerName) ? "" : $" {customerName}";
        return SendAsync(new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "interactive",
            interactive = new
            {
                type = "list",
                header = new { type = "text", text = "🏝️ Paradise Palm Resort" },
                body = new
                {
                    text = $"Welcome{name}! 👋\n\nHow can we help you today?"
                },
                footer = new { text = "Paradise Palm Resort" },
                action = new
                {
                    button = "Explore Resort",
                    sections = new[]
                    {
                        new
                        {
                            title = "Guest Services",
                            rows = new[]
                            {
                                new { id = "BOOK_ROOM", title = "🏨 Book a Room", description = "Find rooms and make a booking" },
                                new { id = "CHECK_AVAILABILITY", title = "📅 Check Availability", description = "Check rooms for your dates" },
                                new { id = "VIEW_PACKAGES", title = "💰 View Packages", description = "See our special packages" },
                                new { id = "LOCATION", title = "📍 Resort Location", description = "Get directions to the resort" },
                                new { id = "CONTACT", title = "📞 Contact Resort", description = "Speak with our team" }
                            }
                        }
                    }
                }
            }
        }, cancellationToken);
    }

    public Task<bool> SendBookingMenuAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "interactive",
            interactive = new
            {
                type = "button",
                body = new
                {
                    text = "🏨 Let's find your perfect room.\n\nPlease use the booking website for date selection, or continue here for assistance."
                },
                action = new
                {
                    buttons = new[]
                    {
                        new { type = "reply", reply = new { id = "BOOKING_WEBSITE", title = "🌐 Book Online" } },
                        new { type = "reply", reply = new { id = "CONTACT", title = "📞 Contact Us" } }
                    }
                }
            }
        }, cancellationToken);
    }

    public async Task<bool> SendRoomListAsync(
        string phoneNumber,
        DateTime checkIn,
        DateTime checkOut,
        int guests,
        CancellationToken cancellationToken = default)
    {
        // The actual availability should come from BookingService.
        // This method is kept for WhatsApp presentation after availability is resolved.
        var nights = Math.Max(1, (checkOut.Date - checkIn.Date).Days);

        return await SendAsync(new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "interactive",
            interactive = new
            {
                type = "list",
                header = new { type = "text", text = "🏨 Available Rooms" },
                body = new
                {
                    text = $"For {guests} guest(s)\n{checkIn:dd MMM} → {checkOut:dd MMM} ({nights} night(s))"
                },
                footer = new { text = "Select a room to continue" },
                action = new
                {
                    button = "View Rooms",
                    sections = new[]
                    {
                        new
                        {
                            title = "Rooms",
                            rows = new[]
                            {
                                new { id = "ROOM_DELUXE", title = "Deluxe Garden Room", description = "₹4,500/night · Up to 2 guests" },
                                new { id = "ROOM_PREMIUM", title = "Premium Pool View", description = "₹6,500/night · Up to 3 guests" },
                                new { id = "ROOM_FAMILY", title = "Family Suite", description = "₹9,000/night · Up to 5 guests" }
                            }
                        }
                    }
                }
            }
        }, cancellationToken);
    }

    public Task<bool> SendBookingSummaryAsync(
        string phoneNumber,
        string bookingReference,
        string roomName,
        DateTime checkIn,
        DateTime checkOut,
        int guests,
        decimal totalAmount,
        CancellationToken cancellationToken = default)
    {
        var text =
            $"✅ Booking request received!\n\n" +
            $"🏝️ Paradise Palm Resort\n" +
            $"🏨 {roomName}\n" +
            $"📅 {checkIn:dd MMM yyyy} → {checkOut:dd MMM yyyy}\n" +
            $"👥 {guests} guest(s)\n" +
            $"💰 ₹{totalAmount:N0}\n\n" +
            $"Booking Reference: *{bookingReference}*\n\n" +
            "Our team will contact you to confirm your reservation.";

        return SendTextAsync(phoneNumber, text, cancellationToken);
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

            logger.LogInformation(
                "WhatsApp message sent successfully. StatusCode: {StatusCode}",
                (int)response.StatusCode);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while calling WhatsApp Graph API.");
            return false;
        }
    }

    private static string NormalizePhone(string phoneNumber) =>
        new string(phoneNumber.Where(char.IsDigit).ToArray());
}

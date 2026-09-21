namespace UplivaResortBooking.Models;

public class WhatsAppSettings
{
    public string GraphApiVersion { get; set; } = "v26.0";
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string WebhookVerifyToken { get; set; } = string.Empty;
}

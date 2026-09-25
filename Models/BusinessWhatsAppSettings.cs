using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class BusinessWhatsAppSettings
{
    public int Id { get; set; }
    public int BusinessId { get; set; }

    [MaxLength(100)]
    public string WabaId { get; set; } = string.Empty;

    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string AboutText { get; set; } = string.Empty;

    [MaxLength(100)]
    public string BusinessCategory { get; set; } = string.Empty;

    [MaxLength(500)]
    public string WelcomeMessage { get; set; } = string.Empty;

    [MaxLength(100)]
    public string PhoneNumberId { get; set; } = string.Empty;

    // Production deployments should encrypt this value at rest or move it to a secret manager.
    public string AccessToken { get; set; } = string.Empty;

    [MaxLength(100)]
    public string WebhookVerifyToken { get; set; } = string.Empty;

    [MaxLength(20)]
    public string GraphApiVersion { get; set; } = "v26.0";

    public bool IsEnabled { get; set; }

    /// <summary>Maximum number of business-selected products to showcase in WhatsApp.</summary>
    public int FeaturedProductLimit { get; set; } = 6;
}

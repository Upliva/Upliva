using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class BusinessWhatsAppSettings
{
    public int Id { get; set; }
    public int BusinessId { get; set; }

    [MaxLength(100)]
    public string WabaId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string PhoneNumberId { get; set; } = string.Empty;

    // Store production secrets outside source control. This field is encrypted/secret-managed in the production roadmap.
    public string AccessToken { get; set; } = string.Empty;

    [MaxLength(100)]
    public string WebhookVerifyToken { get; set; } = string.Empty;

    [MaxLength(20)]
    public string GraphApiVersion { get; set; } = "v26.0";

    public bool IsEnabled { get; set; }

    /// <summary>Maximum number of business-selected products to showcase in WhatsApp.</summary>
    public int FeaturedProductLimit { get; set; } = 6;
}

using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class BusinessIntegrationViewModel
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string ServicePlan { get; set; } = string.Empty;

    [Display(Name = "WABA ID"), MaxLength(100)]
    public string WabaId { get; set; } = string.Empty;

    [Display(Name = "Phone Number ID"), MaxLength(100)]
    public string PhoneNumberId { get; set; } = string.Empty;

    [Display(Name = "Access Token")]
    public string AccessToken { get; set; } = string.Empty;

    [Display(Name = "Webhook Verify Token"), MaxLength(100)]
    public string WebhookVerifyToken { get; set; } = string.Empty;

    [Required, Display(Name = "Graph API Version"), MaxLength(20)]
    public string GraphApiVersion { get; set; } = "v26.0";

    [Display(Name = "Enable WhatsApp integration")]
    public bool IsEnabled { get; set; }

    [Range(1, 50), Display(Name = "Maximum WhatsApp products")]
    public int FeaturedProductLimit { get; set; } = 6;

    // Checked products are sent to WhatsApp. Their Rank determines the display order.
    public List<int> SelectedWhatsAppProductIds { get; set; } = new();
    public Dictionary<int, int> WhatsAppProductRanks { get; set; } = new();
    public List<WhatsAppFeaturedProductViewModel> WhatsAppProducts { get; set; } = new();
}

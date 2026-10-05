using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class WhatsAppTemplateConfiguration
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string ProviderTemplateName { get; set; } = string.Empty;
    [MaxLength(80)] public string Provider { get; set; } = "Gupshup";
    [MaxLength(20)] public string LanguageCode { get; set; } = "en_US";
    [MaxLength(30)] public string Category { get; set; } = "MARKETING";
    [MaxLength(2000)] public string BodyText { get; set; } = string.Empty;
    [MaxLength(80)] public string ButtonText { get; set; } = "View Details";
    [MaxLength(500)] public string ButtonUrlTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

/// <summary>
/// Public lead-capture form. This model is intentionally separate from the
/// post-confirmation business provisioning model so legacy account fields such
/// as Password can never participate in public registration validation.
/// </summary>
public sealed class LeadRegistrationViewModel
{
    [Required, MaxLength(180)]
    [Display(Name = "Business name")]
    public string? Name { get; set; }
    [Required, MaxLength(80)]
    [Display(Name = "Business category")]
    public string? BusinessType { get; set; }
    [Required, MaxLength(10)]
    [Display(Name = "WhatsApp / mobile number")]
    public string? WhatsAppNumber { get; set; }
}

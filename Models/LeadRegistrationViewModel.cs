using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

/// <summary>
/// Public lead-capture form. This model is intentionally separate from the
/// post-confirmation business provisioning model so legacy account fields such
/// as Password can never participate in public registration validation.
/// </summary>
public sealed class LeadRegistrationViewModel
{
    [Required(ErrorMessage = "Please enter your name.")]
    [MaxLength(150)]
    [Display(Name = "Your name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select your business category.")]
    [MaxLength(80)]
    [Display(Name = "Business category")]
    public string BusinessType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your WhatsApp / phone number.")]
    [MaxLength(10)]
    [Display(Name = "WhatsApp / mobile number")]
    public string WhatsAppNumber { get; set; } = string.Empty;
}

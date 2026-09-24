using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class CreateBusinessFromLeadViewModel
{
    public long LeadId { get; set; }

    [Required, MaxLength(180)]
    [Display(Name = "Business name")]
    public string BusinessName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    [Display(Name = "Owner name")]
    public string OwnerName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    [Display(Name = "Business category")]
    public string BusinessType { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    [Display(Name = "WhatsApp / phone")]
    public string WhatsAppNumber { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    [Display(Name = "Upliva plan")]
    public string ServicePlan { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class WhatsAppTestViewModel
{
    [Required]
    [Display(Name = "WhatsApp number")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Message { get; set; }

    public string? Result { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class WhatsAppTemplateParameterViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class WhatsAppTemplateSendViewModel
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateBody { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Business")]
    public int BusinessId { get; set; }

    [Required]
    [Display(Name = "Recipient WhatsApp number")]
    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Enter a valid WhatsApp number with country code, for example 919798555992.")]
    public string Destination { get; set; } = string.Empty;

    public List<WhatsAppTemplateParameterViewModel> Parameters { get; set; } = [];

    public string? Result { get; set; }
    public bool Success { get; set; }
}

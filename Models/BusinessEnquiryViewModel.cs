using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class BusinessEnquiryViewModel
{
    public int BusinessId { get; set; }
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, Phone, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
}

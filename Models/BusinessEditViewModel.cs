using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class BusinessEditViewModel
{
    public int Id { get; set; }

    [MaxLength(180)]
    [Display(Name = "Business name")]
    public string? Name { get; set; }

    [MaxLength(80)]
    [Display(Name = "Business type")]
    public string? BusinessType { get; set; }

    [MaxLength(40)]
    [Display(Name = "Upliva plan")]
    public string? ServicePlan { get; set; }

    [MaxLength(150)]
    [Display(Name = "Owner name")]
    public string? OwnerName { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(30)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [MaxLength(30)]
    [Display(Name = "WhatsApp number")]
    public string? WhatsAppNumber { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    [Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(500)]
    [Display(Name = "Business hours")]
    public string? BusinessHours { get; set; }

    [MaxLength(250)]
    public string? Tagline { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500), Url]
    [Display(Name = "Logo URL")]
    public string? LogoUrl { get; set; }

    // Read-only/system information shown on the edit page.
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CatalogTemplateKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
}

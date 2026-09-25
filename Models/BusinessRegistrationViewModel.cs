using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

/// <summary>
/// Public registration is lead-only. The public form intentionally binds only
/// Name, BusinessType and WhatsAppNumber. The legacy optional properties remain
/// here for compatibility with the existing business/account provisioning services;
/// they are not rendered or required by public registration.
/// </summary>
public class BusinessRegistrationViewModel
{
    [MaxLength(150), Display(Name = "Your name")]
public string? Name { get; set; }

    [MaxLength(80), Display(Name = "Business category")]
public string? BusinessType { get; set; }

    [MaxLength(30), Display(Name = "WhatsApp / phone number")]
public string? WhatsAppNumber { get; set; }

    // Kept only so the existing post-confirmation provisioning services continue
    // to compile and can be reused when the admin creates a real Business later.
    [MaxLength(180)] public string? BusinessName { get; set; }
    [MaxLength(180)] public string? WebsiteTitle { get; set; }
    [MaxLength(150)] public string? OwnerName { get; set; }
    [EmailAddress, MaxLength(200)] public string? Email { get; set; }
    [MaxLength(30)] public string? PhoneNumber { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(100)] public string? State { get; set; }
    [MaxLength(20)] public string? PostalCode { get; set; }
    [MaxLength(100)] public string? Country { get; set; }
    [MaxLength(500)] public string? BusinessHours { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
    [MinLength(8), DataType(DataType.Password)] public string? Password { get; set; }
    [Compare(nameof(Password)), DataType(DataType.Password)] public string? ConfirmPassword { get; set; }
}

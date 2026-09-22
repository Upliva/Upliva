using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public class BusinessRegistrationViewModel
{
    [Required, MaxLength(180), Display(Name = "Business name")]
    public string BusinessName { get; set; } = string.Empty;

    [Required, Display(Name = "Business type")]
    public string BusinessType { get; set; } = string.Empty;

    [Required, MaxLength(150), Display(Name = "Owner name")]
    public string OwnerName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Phone, Display(Name = "WhatsApp number")]
    public string WhatsAppNumber { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

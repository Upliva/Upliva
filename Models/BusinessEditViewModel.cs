using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public class BusinessEditViewModel
{
    public int BusinessId { get; set; }

    [Required, MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string BusinessType { get; set; } = string.Empty;

    [MaxLength(150)]
    public string OwnerName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Phone, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Phone, MaxLength(30)]
    public string WhatsAppNumber { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Tagline { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string LogoUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string HeroImageUrl { get; set; } = string.Empty;
}

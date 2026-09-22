using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public static class PlatformRoles
{
    public const string Admin = "Admin";
    public const string BusinessOwner = "BusinessOwner";
}

public class PlatformUser
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Role { get; set; } = PlatformRoles.BusinessOwner;

    public int? BusinessId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

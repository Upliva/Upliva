using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public class Package
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public int DurationNights { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

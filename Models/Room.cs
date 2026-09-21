using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public class Room
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }
    public int MaxGuests { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

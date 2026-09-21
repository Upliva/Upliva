using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public class Booking
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string BookingReference { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string GuestName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Email { get; set; }

    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Guests { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public decimal TotalAmount { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

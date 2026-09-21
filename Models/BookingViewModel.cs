using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public class BookingViewModel
{
    [Required, Display(Name = "Guest name")]
    public string GuestName { get; set; } = string.Empty;

    [Required, Phone, Display(Name = "WhatsApp / phone")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Check-in")]
    public DateTime CheckIn { get; set; } = DateTime.Today.AddDays(1);

    [Required, DataType(DataType.Date), Display(Name = "Check-out")]
    public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(2);

    [Range(1, 20)]
    public int Guests { get; set; } = 2;

    [Required]
    public int RoomId { get; set; }

    public string? Notes { get; set; }

    public List<Room> AvailableRooms { get; set; } = [];
}

namespace UplivaResortBooking.Models;

public class BookingConfirmationViewModel
{
    public Booking Booking { get; set; } = null!;
    public ResortInfo Resort { get; set; } = null!;
}

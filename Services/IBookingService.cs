using UplivaResortBooking.Models;

namespace UplivaResortBooking.Services;

public interface IBookingService
{
    Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int guests);
    Task<Booking> CreateBookingAsync(BookingViewModel model);
}

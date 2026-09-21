using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;
using UplivaResortBooking.Models;

namespace UplivaResortBooking.Services;

public class BookingService(
    ResortDbContext db,
    ILogger<BookingService> logger) : IBookingService
{
    public async Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int guests)
    {
        logger.LogInformation(
            "Checking room availability. CheckIn: {CheckIn}, CheckOut: {CheckOut}, Guests: {Guests}",
            checkIn.Date, checkOut.Date, guests);

        if (checkOut.Date <= checkIn.Date)
        {
            logger.LogWarning("Invalid date range. CheckIn: {CheckIn}, CheckOut: {CheckOut}", checkIn, checkOut);
            return [];
        }

        var bookedRoomIds = await db.Bookings
            .Where(b => b.Status != "Cancelled"
                        && b.CheckIn < checkOut
                        && b.CheckOut > checkIn)
            .Select(b => b.RoomId)
            .ToListAsync();

        var rooms = await db.Rooms
            .Where(r => r.IsActive
                        && r.MaxGuests >= guests
                        && !bookedRoomIds.Contains(r.Id))
            .OrderBy(r => r.PricePerNight)
            .ToListAsync();

        logger.LogInformation("Availability check returned {RoomCount} room(s).", rooms.Count);
        return rooms;
    }

    public async Task<Booking> CreateBookingAsync(BookingViewModel model)
    {
        logger.LogInformation(
            "Creating booking. Guest: {GuestName}, Phone: {PhoneNumber}, RoomId: {RoomId}",
            model.GuestName, model.PhoneNumber, model.RoomId);

        if (model.CheckOut.Date <= model.CheckIn.Date)
            throw new InvalidOperationException("Check-out must be after check-in.");

        var availableRooms = await GetAvailableRoomsAsync(model.CheckIn.Date, model.CheckOut.Date, model.Guests);
        if (!availableRooms.Any(r => r.Id == model.RoomId))
            throw new InvalidOperationException("The selected room is no longer available for these dates.");

        var room = availableRooms.First(r => r.Id == model.RoomId);
        var nights = (model.CheckOut.Date - model.CheckIn.Date).Days;

        var booking = new Booking
        {
            BookingReference = $"PP{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            GuestName = model.GuestName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Email = model.Email?.Trim(),
            CheckIn = model.CheckIn.Date,
            CheckOut = model.CheckOut.Date,
            Guests = model.Guests,
            RoomId = room.Id,
            TotalAmount = room.PricePerNight * nights,
            Status = "Pending",
            Notes = model.Notes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        await db.Entry(booking).Reference(x => x.Room).LoadAsync();

        logger.LogInformation(
            "Booking created successfully. Reference: {BookingReference}, Total: {TotalAmount}",
            booking.BookingReference, booking.TotalAmount);

        return booking;
    }
}

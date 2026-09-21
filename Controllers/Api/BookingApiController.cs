using Microsoft.AspNetCore.Mvc;
using UplivaResortBooking.Models;
using UplivaResortBooking.Services;

namespace UplivaResortBooking.Controllers.Api;

[ApiController]
[Route("api/booking")]
public class BookingApiController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("availability")]
    public async Task<IActionResult> Availability(
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut,
        [FromQuery] int guests = 2)
    {
        var rooms = await bookingService.GetAvailableRoomsAsync(checkIn.Date, checkOut.Date, guests);

        return Ok(rooms.Select(r => new
        {
            r.Id,
            r.Name,
            r.Description,
            r.PricePerNight,
            r.MaxGuests,
            r.ImageUrl
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookingViewModel model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var booking = await bookingService.CreateBookingAsync(model);
            return Ok(new
            {
                booking.BookingReference,
                booking.GuestName,
                booking.PhoneNumber,
                booking.CheckIn,
                booking.CheckOut,
                booking.Guests,
                Room = booking.Room?.Name,
                booking.TotalAmount,
                booking.Status
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

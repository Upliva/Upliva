using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;
using UplivaResortBooking.Models;
using UplivaResortBooking.Services;

namespace UplivaResortBooking.Controllers;

public class BookingController(
    IBookingService bookingService,
    ResortDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? checkIn, DateTime? checkOut, int guests = 2)
    {
        var start = (checkIn ?? DateTime.Today.AddDays(1)).Date;
        var end = (checkOut ?? start.AddDays(1)).Date;

        var model = new BookingViewModel
        {
            CheckIn = start,
            CheckOut = end,
            Guests = Math.Clamp(guests, 1, 20),
            AvailableRooms = await bookingService.GetAvailableRoomsAsync(start, end, Math.Clamp(guests, 1, 20))
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BookingViewModel model)
    {
        model.AvailableRooms = await bookingService.GetAvailableRoomsAsync(
            model.CheckIn.Date,
            model.CheckOut.Date,
            model.Guests);

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var booking = await bookingService.CreateBookingAsync(model);
            return RedirectToAction(nameof(Confirmation), new { id = booking.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.AvailableRooms = await bookingService.GetAvailableRoomsAsync(
                model.CheckIn.Date,
                model.CheckOut.Date,
                model.Guests);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var booking = await db.Bookings
            .Include(x => x.Room)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (booking is null)
            return NotFound();

        var resort = await db.ResortInfo.FirstAsync();
        return View(new BookingConfirmationViewModel
        {
            Booking = booking,
            Resort = resort
        });
    }
}

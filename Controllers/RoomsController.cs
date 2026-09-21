using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;

namespace UplivaResortBooking.Controllers;

public class RoomsController(ResortDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var rooms = await db.Rooms.Where(x => x.IsActive).OrderBy(x => x.PricePerNight).ToListAsync();
        return View(rooms);
    }
}

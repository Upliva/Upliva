using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;

namespace UplivaResortBooking.Controllers;

public class HomeController(ResortDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Resort = await db.ResortInfo.FirstAsync();
        ViewBag.Rooms = await db.Rooms.Where(x => x.IsActive).OrderBy(x => x.PricePerNight).Take(3).ToListAsync();
        ViewBag.Packages = await db.Packages.Where(x => x.IsActive).Take(3).ToListAsync();
        return View();
    }
}

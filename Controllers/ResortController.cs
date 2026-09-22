using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;

namespace UplivaResortBooking.Controllers;

public class ResortController(ResortDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Resort = await db.ResortInfo.AsNoTracking().FirstAsync(cancellationToken);
        ViewBag.Rooms = await db.Rooms.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.PricePerNight).Take(3).ToListAsync(cancellationToken);
        ViewBag.Packages = await db.Packages.AsNoTracking().Where(x => x.IsActive).Take(3).ToListAsync(cancellationToken);
        return View();
    }
}

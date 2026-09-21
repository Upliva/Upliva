using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;

namespace UplivaResortBooking.Controllers;

public class PackagesController(ResortDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var packages = await db.Packages.Where(x => x.IsActive).OrderBy(x => x.Price).ToListAsync();
        return View(packages);
    }
}

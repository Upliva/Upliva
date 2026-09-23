using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Controllers;

[Authorize(Roles = PlatformRoles.BusinessOwner)]
public class BusinessDashboardController(UplivaDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var businessId = GetBusinessId();
        if (businessId is null)
            return Forbid();

        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null)
            return NotFound();

        ViewBag.CatalogCount = await db.BusinessCatalogItems.CountAsync(x => x.BusinessId == business.Id, cancellationToken);
        ViewBag.OfferCount = await db.BusinessOffers.CountAsync(x => x.BusinessId == business.Id, cancellationToken);
        ViewBag.EnquiryCount = await db.BusinessEnquiries.CountAsync(x => x.BusinessId == business.Id, cancellationToken);

        return View(business);
    }

    private int? GetBusinessId()
    {
        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

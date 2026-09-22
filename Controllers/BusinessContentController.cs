using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;
using UplivaResortBooking.Models;

namespace UplivaResortBooking.Controllers;

[Authorize(Roles = "Admin,BusinessOwner")]
public class BusinessContentController(ResortDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(id);
        if (businessId is null) return Forbid();
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();
        ViewBag.Business = business;
        ViewBag.Catalog = await db.BusinessCatalogItems.AsNoTracking().Where(x => x.BusinessId == business.Id).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        ViewBag.Offers = await db.BusinessOffers.AsNoTracking().Where(x => x.BusinessId == business.Id).OrderByDescending(x => x.Id).ToListAsync(cancellationToken);
        return View(new BusinessCatalogItemViewModel { BusinessId = business.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCatalog(BusinessCatalogItemViewModel model, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(model.BusinessId);
        if (businessId is null) return Forbid();
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index), new { id = businessId });
        db.BusinessCatalogItems.Add(new BusinessCatalogItem { BusinessId = businessId.Value, Name = model.Name.Trim(), Description = model.Description.Trim(), PriceText = model.PriceText.Trim(), ImageUrl = model.ImageUrl.Trim(), Category = model.Category.Trim(), IsActive = true, SortOrder = 0 });
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { id = businessId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOffer(BusinessOfferViewModel model, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(model.BusinessId);
        if (businessId is null) return Forbid();
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index), new { id = businessId });
        db.BusinessOffers.Add(new BusinessOffer { BusinessId = businessId.Value, Title = model.Title.Trim(), Description = model.Description.Trim(), ImageUrl = model.ImageUrl.Trim(), DiscountText = model.DiscountText.Trim(), IsPublished = User.IsInRole(PlatformRoles.Admin) });
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { id = businessId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishOffer(int id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(PlatformRoles.Admin)) return Forbid();
        var offer = await db.BusinessOffers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (offer is null) return NotFound();
        offer.IsPublished = true;
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { id = offer.BusinessId });
    }

    private int? ResolveBusinessId(int? requestedId)
    {
        if (User.IsInRole(PlatformRoles.Admin) && requestedId.HasValue)
            return requestedId;
        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

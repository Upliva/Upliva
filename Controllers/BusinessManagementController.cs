using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;
using UplivaResortBooking.Models;

namespace UplivaResortBooking.Controllers;

[Authorize(Roles = "Admin,BusinessOwner")]
public class BusinessManagementController(ResortDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(id);
        if (businessId is null) return Forbid();
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();
        return View(new BusinessEditViewModel
        {
            BusinessId = business.Id, Name = business.Name, BusinessType = business.BusinessType,
            OwnerName = business.OwnerName, Email = business.Email, PhoneNumber = business.PhoneNumber,
            WhatsAppNumber = business.WhatsAppNumber, Address = business.Address, City = business.City,
            Tagline = business.Tagline, Description = business.Description, LogoUrl = business.LogoUrl,
            HeroImageUrl = business.HeroImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BusinessEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var businessId = ResolveBusinessId(model.BusinessId);
        if (businessId is null) return Forbid();
        var business = await db.Businesses.FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();

        business.Name = model.Name.Trim();
        business.BusinessType = model.BusinessType.Trim();
        business.OwnerName = model.OwnerName.Trim();
        business.Email = model.Email.Trim();
        business.PhoneNumber = model.PhoneNumber.Trim();
        business.WhatsAppNumber = model.WhatsAppNumber.Trim();
        business.Address = model.Address.Trim();
        business.City = model.City.Trim();
        business.Tagline = model.Tagline.Trim();
        business.Description = model.Description.Trim();
        business.LogoUrl = model.LogoUrl.Trim();
        business.HeroImageUrl = model.HeroImageUrl.Trim();

        await db.SaveChangesAsync(cancellationToken);
        return User.IsInRole(PlatformRoles.Admin)
            ? RedirectToAction("Index", "AdminDashboard")
            : RedirectToAction("Index", "BusinessDashboard");
    }

    private int? ResolveBusinessId(int? requestedId)
    {
        if (User.IsInRole(PlatformRoles.Admin) && requestedId.HasValue)
            return requestedId;
        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Authorize(Roles = PlatformRoles.BusinessOwner)]
public class BusinessDashboardController(UplivaDbContext db, ICatalogTemplateService catalogTemplateService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var businessId = GetBusinessId();
        if (businessId is null) return Forbid();

        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();
        if (business.Status != BusinessStatuses.Approved) return Forbid();

        ViewBag.CatalogCount = await db.BusinessCatalogItems.CountAsync(x => x.BusinessId == business.Id && x.IsActive, cancellationToken);
        ViewBag.WhatsAppSelectedCount = await db.BusinessCatalogItems.CountAsync(x => x.BusinessId == business.Id && x.IsActive && x.IsWhatsAppTopPick, cancellationToken);
        ViewBag.EnquiryCount = await db.BusinessEnquiries.CountAsync(x => x.BusinessId == business.Id, cancellationToken);
        ViewBag.MessageCount = await db.WhatsAppMessageLogs.CountAsync(x => x.BusinessId == business.Id, cancellationToken);
        ViewBag.FollowUpCount = await db.BusinessFollowUps.CountAsync(x => x.BusinessId == business.Id && x.Status != FollowUpStatuses.Completed, cancellationToken);
        ViewBag.CatalogTemplateKey = string.IsNullOrWhiteSpace(business.CatalogTemplateKey) || business.CatalogTemplateKey.Equals("generic", StringComparison.OrdinalIgnoreCase)
            ? catalogTemplateService.GetTemplate(business.BusinessType).Key
            : business.CatalogTemplateKey;
        ViewBag.WhatsAppEnabled = await db.BusinessWhatsAppSettings.AnyAsync(x => x.BusinessId == business.Id && x.IsEnabled, cancellationToken);

        return View(business);
    }

    private int? GetBusinessId()
    {
        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

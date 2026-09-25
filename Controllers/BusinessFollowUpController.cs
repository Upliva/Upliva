using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Authorize(Roles = "Admin,BusinessOwner")]
public class BusinessFollowUpController(UplivaDbContext db, IAuditLogService auditLogService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id, CancellationToken cancellationToken)
    {
        var businessId = await ResolveBusinessIdAsync(id, cancellationToken);
        if (businessId is null) return Forbid();
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId, cancellationToken);
        if (business is null) return NotFound();
        var followUps = await db.BusinessFollowUps.AsNoTracking().Where(x => x.BusinessId == businessId).OrderBy(x => x.Status == FollowUpStatuses.Completed).ThenBy(x => x.DueAtUtc).ThenByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        ViewBag.Business = business;
        return View(followUps);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int id, int? enquiryId, CancellationToken cancellationToken)
    {
        var businessId = await ResolveBusinessIdAsync(id, cancellationToken);
        if (businessId is null) return Forbid();
        var enquiry = enquiryId.HasValue ? await db.BusinessEnquiries.AsNoTracking().FirstOrDefaultAsync(x => x.BusinessId == businessId && x.Id == enquiryId, cancellationToken) : null;
        ViewBag.Business = await db.Businesses.AsNoTracking().FirstAsync(x => x.Id == businessId, cancellationToken);
        return View(new BusinessFollowUp
        { BusinessId = businessId.Value, EnquiryId = enquiry?.Id, CatalogItemId = enquiry?.CatalogItemId, CustomerName = enquiry?.Name ?? string.Empty, CustomerPhoneNumber = enquiry?.PhoneNumber ?? string.Empty, Notes = enquiry?.Message ?? string.Empty, DueAtUtc = DateTime.UtcNow.AddDays(1) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BusinessFollowUp model, CancellationToken cancellationToken)
    {
        var businessId = await ResolveBusinessIdAsync(model.BusinessId, cancellationToken);
        if (businessId is null) return Forbid();
        model.BusinessId = businessId.Value;
        if (!ModelState.IsValid) { ViewBag.Business = await db.Businesses.AsNoTracking().FirstAsync(x => x.Id == businessId, cancellationToken); return View(model); }
        model.Id = 0; model.CreatedAtUtc = DateTime.UtcNow; model.UpdatedAtUtc = DateTime.UtcNow;
        db.BusinessFollowUps.Add(model);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync("FollowUpCreated", "BusinessFollowUp", model.Id.ToString(), model.BusinessId, System.Text.Json.JsonSerializer.Serialize(new { model.CustomerName, model.CustomerPhoneNumber }), cancellationToken);
        TempData["ToastType"] = "success"; TempData["ToastMessage"] = "Follow-up created.";
        return RedirectToAction(nameof(Index), new { id = model.BusinessId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(long id, CancellationToken cancellationToken)
    {
        var item = await db.BusinessFollowUps.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();
        var businessId = await ResolveBusinessIdAsync(item.BusinessId, cancellationToken);
        if (businessId is null) return Forbid();
        item.Status = FollowUpStatuses.Completed; item.CompletedAtUtc = DateTime.UtcNow; item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync("FollowUpCompleted", "BusinessFollowUp", item.Id.ToString(), item.BusinessId, "{\"status\":\"Completed\"}", cancellationToken);
        return RedirectToAction(nameof(Index), new { id = item.BusinessId });
    }

    private async Task<int?> ResolveBusinessIdAsync(int? requestedId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(PlatformRoles.Admin) && requestedId.HasValue) return await db.Businesses.AnyAsync(x => x.Id == requestedId.Value, cancellationToken) ? requestedId.Value : null;
        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

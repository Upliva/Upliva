using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Controllers;

[Authorize(Roles = "Admin,BusinessOwner")]
public class BusinessEnquiryController(UplivaDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id, CancellationToken cancellationToken)
    {
        var businessId = await ResolveBusinessIdAsync(id, cancellationToken);
        if (businessId is null) return Forbid();
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId, cancellationToken);
        if (business is null) return NotFound();

        // Left join preserves general enquiries while showing the correct product/category when selected.
        var enquiries = await (
            from enquiry in db.BusinessEnquiries.AsNoTracking()
            where enquiry.BusinessId == businessId.Value
            join product in db.BusinessCatalogItems.AsNoTracking()
                on new { Id = enquiry.CatalogItemId, enquiry.BusinessId }
                equals new { Id = (int?)product.Id, product.BusinessId } into productJoin
            from product in productJoin.DefaultIfEmpty()
            orderby enquiry.CreatedAtUtc descending
            select new BusinessEnquiryListItemViewModel
            {
                Id = enquiry.Id,
                BusinessId = enquiry.BusinessId,
                Name = enquiry.Name,
                PhoneNumber = enquiry.PhoneNumber,
                Email = enquiry.Email ?? string.Empty,
                Message = enquiry.Message,
                Status = enquiry.Status,
                Source = enquiry.Source,
                CatalogItemId = enquiry.CatalogItemId,
                ProductName = product == null ? string.Empty : product.Name,
                ProductCategory = product == null ? string.Empty : product.Category,
                CreatedAtUtc = enquiry.CreatedAtUtc
            }).ToListAsync(cancellationToken);

        var callClicks = await db.CallEvents.AsNoTracking().Where(x => x.BusinessId == businessId).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);
        var defaultWhatsAppTemplate = await db.WhatsAppTemplateConfigurations.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);
        ViewBag.Business = business;
        ViewBag.CallClicks = callClicks;
        ViewBag.DefaultWhatsAppTemplate = defaultWhatsAppTemplate;
        ViewBag.EnquiryStatuses = new[] { "New", "Pending", "Completed" };
        return View(enquiries);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, int? businessId, CancellationToken cancellationToken)
    {
        var allowedStatuses = new[] { "New", "Pending", "Completed" };
        if (string.IsNullOrWhiteSpace(status) || !allowedStatuses.Contains(status, StringComparer.Ordinal))
        {
            TempData["EnquiryStatusError"] = "Please select a valid enquiry status.";
            return RedirectToAction(nameof(Index), new { id = businessId });
        }

        var resolvedBusinessId = await ResolveBusinessIdAsync(businessId, cancellationToken);
        if (resolvedBusinessId is null) return Forbid();
        var enquiry = await db.BusinessEnquiries.FirstOrDefaultAsync(x => x.Id == id && x.BusinessId == resolvedBusinessId.Value, cancellationToken);
        if (enquiry is null) return NotFound();

        enquiry.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        TempData["EnquiryStatusSuccess"] = "Enquiry status updated.";
        return RedirectToAction(nameof(Index), new { id = resolvedBusinessId.Value });
    }

    private async Task<int?> ResolveBusinessIdAsync(int? requestedId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(PlatformRoles.Admin) && requestedId.HasValue) return await db.Businesses.AnyAsync(x => x.Id == requestedId.Value, cancellationToken) ? requestedId.Value : null;
        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

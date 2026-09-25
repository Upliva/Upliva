using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Authorize(Roles = "Admin,BusinessOwner")]
public class BusinessManagementController(
    UplivaDbContext db,
    IAuditLogService auditLogService,
    IBusinessCacheService businessCache,
    ICatalogTemplateService catalogTemplateService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (business is null) return NotFound();
        if (!await CanManageAsync(business.Id, cancellationToken)) return Forbid();

        return View(ToViewModel(business));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BusinessEditViewModel model, CancellationToken cancellationToken)
    {
        var business = await db.Businesses
            .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

        if (business is null) return NotFound();
        if (!await CanManageAsync(business.Id, cancellationToken)) return Forbid();

        // The edit screen intentionally does not allow changing system fields such as
        // Id, Slug, Status or approval timestamps. They remain controlled by the platform.
        var requestedType = BusinessInputRules.ResolveBusinessType(model.BusinessType);
        var requestedPlan = BusinessInputRules.ResolvePlan(model.ServicePlan);

        if (!BusinessTypeOptions.All.Contains(requestedType, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(model.BusinessType), "Select a valid business type.");

        if (!string.IsNullOrWhiteSpace(requestedPlan) &&
            !BusinessServicePlans.All.Contains(requestedPlan, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.ServicePlan), "Select a valid Upliva plan.");

        var requestedWhatsApp = BusinessInputRules.NormalizeWhatsApp(model.WhatsAppNumber);
        if (!BusinessInputRules.IsOptionalWhatsAppValid(model.WhatsAppNumber))
            ModelState.AddModelError(nameof(model.WhatsAppNumber), "If supplied, WhatsApp must be a valid 10-digit Indian number.");
        else if (!string.IsNullOrWhiteSpace(requestedWhatsApp))
        {
            var phoneVariants = BusinessInputRules.GetWhatsAppVariants(requestedWhatsApp);
            var duplicateWhatsApp = await db.Businesses.AnyAsync(
                x => x.Id != business.Id &&
                     phoneVariants.Contains(x.WhatsAppNumber) &&
                     !string.IsNullOrWhiteSpace(x.WhatsAppNumber),
                cancellationToken);

            if (duplicateWhatsApp)
                ModelState.AddModelError(nameof(model.WhatsAppNumber), "Another business already uses this WhatsApp number.");
        }

        if (!ModelState.IsValid)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
            model.BusinessType = requestedType;
            model.ServicePlan = requestedPlan;
            model.OwnerName = model.OwnerName?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim() ?? string.Empty;
            model.PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty;
            model.WhatsAppNumber = model.WhatsAppNumber?.Trim() ?? string.Empty;
            model.Address = model.Address?.Trim() ?? string.Empty;
            model.City = model.City?.Trim() ?? string.Empty;
            model.State = model.State?.Trim() ?? string.Empty;
            model.PostalCode = model.PostalCode?.Trim() ?? string.Empty;
            model.Country = model.Country?.Trim() ?? string.Empty;
            model.BusinessHours = model.BusinessHours?.Trim() ?? string.Empty;
            model.Tagline = model.Tagline?.Trim() ?? string.Empty;
            model.Description = model.Description?.Trim() ?? string.Empty;
            model.LogoUrl = model.LogoUrl?.Trim() ?? string.Empty;
            model.Slug = business.Slug;
            model.Status = business.Status;
            model.CatalogTemplateKey = business.CatalogTemplateKey;
            model.CreatedAtUtc = business.CreatedAtUtc;
            model.ApprovedAtUtc = business.ApprovedAtUtc;
            return View(model);
        }

        var oldBusinessType = business.BusinessType;
        var oldName = business.Name;

        business.Name = BusinessInputRules.ResolveBusinessName(model.Name, business.Name);
        business.BusinessType = requestedType;
        business.ServicePlan = requestedPlan;
        business.OwnerName = BusinessInputRules.Clean(model.OwnerName);
        business.Email = BusinessInputRules.Clean(model.Email);
        business.PhoneNumber = BusinessInputRules.Clean(model.PhoneNumber);
        business.WhatsAppNumber = requestedWhatsApp;
        business.Address = BusinessInputRules.Clean(model.Address);
        business.City = BusinessInputRules.Clean(model.City);
        business.State = BusinessInputRules.Clean(model.State);
        business.PostalCode = BusinessInputRules.Clean(model.PostalCode);
        business.Country = string.IsNullOrWhiteSpace(model.Country) ? "India" : BusinessInputRules.Clean(model.Country);
        business.BusinessHours = BusinessInputRules.Clean(model.BusinessHours);
        business.Tagline = BusinessInputRules.Clean(model.Tagline);
        business.Description = BusinessInputRules.Clean(model.Description);
        business.LogoUrl = BusinessInputRules.Clean(model.LogoUrl);

        // Keep the dynamic catalog engine aligned with the business type. No DB schema
        // change is required because the template key is already stored on Business.
        business.CatalogTemplateKey = catalogTemplateService.GetTemplate(requestedType).Key;

        // A converted Interested lead and its Business represent the same customer record.
        // Keep the lead snapshot synchronized so the lead table never shows a stale business name/type/WhatsApp.
        var linkedLead = await db.ChatbotLeads
            .FirstOrDefaultAsync(x => x.ConvertedBusinessId == business.Id, cancellationToken);
        if (linkedLead is not null)
        {
            linkedLead.Name = business.Name;
            linkedLead.BusinessType = business.BusinessType;
            linkedLead.WhatsAppNumber = business.WhatsAppNumber;
        }

        await db.SaveChangesAsync(cancellationToken);

        // Re-read the row from SQL Server before showing success. This makes the edit
        // behavior deterministic during local testing and catches wrong-DB connections.
        var saved = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == business.Id, cancellationToken);

        if (saved is null ||
            !string.Equals(saved.Name, business.Name, StringComparison.Ordinal) ||
            !string.Equals(saved.BusinessType, business.BusinessType, StringComparison.Ordinal) ||
            !string.Equals(saved.WhatsAppNumber, business.WhatsAppNumber, StringComparison.Ordinal) ||
            !string.Equals(saved.CatalogTemplateKey, business.CatalogTemplateKey, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "The business update could not be verified in SQL Server. Please check the active database connection and try again.");
            var failed = ToViewModel(business);
            return View(failed);
        }

        businessCache.InvalidateBusiness(business.Id);
        await auditLogService.WriteAsync(
            AuditActions.BusinessProfileChanged,
            "Business",
            business.Id.ToString(),
            business.Id,
            JsonSerializer.Serialize(new
            {
                oldName,
                oldBusinessType,
                newName = business.Name,
                newBusinessType = business.BusinessType,
                template = business.CatalogTemplateKey
            }),
            cancellationToken);

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"{business.Name} business details were updated successfully.";

        return RedirectToAction("Index", "AdminMarketing");
    }

    private async Task<bool> CanManageAsync(int businessId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(PlatformRoles.Admin))
            return await db.Businesses.AsNoTracking().AnyAsync(x => x.Id == businessId, cancellationToken);

        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var ownerBusinessId) && ownerBusinessId == businessId;
    }

    private static BusinessEditViewModel ToViewModel(Business business) => new()
    {
        Id = business.Id,
        Name = business.Name,
        BusinessType = business.BusinessType,
        ServicePlan = business.ServicePlan,
        OwnerName = business.OwnerName,
        Email = business.Email,
        PhoneNumber = business.PhoneNumber,
        WhatsAppNumber = business.WhatsAppNumber,
        Address = business.Address,
        City = business.City,
        State = business.State,
        PostalCode = business.PostalCode,
        Country = business.Country,
        BusinessHours = business.BusinessHours,
        Tagline = business.Tagline,
        Description = business.Description,
        LogoUrl = business.LogoUrl,
        Slug = business.Slug,
        Status = business.Status,
        CatalogTemplateKey = business.CatalogTemplateKey,
        CreatedAtUtc = business.CreatedAtUtc,
        ApprovedAtUtc = business.ApprovedAtUtc
    };
}

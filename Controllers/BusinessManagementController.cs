using System.Security.Claims;
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
    ILogger<BusinessManagementController> logger,
    IBusinessBrochureService brochureService) : Controller
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
            State = business.State, PostalCode = business.PostalCode, Country = business.Country, BusinessHours = business.BusinessHours,
            Tagline = business.Tagline, Description = business.Description, LogoUrl = business.LogoUrl,
            HeroImageUrl = business.HeroImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BusinessEditViewModel model, CancellationToken cancellationToken)
    {
        // Business owners are always scoped to the BusinessId claim.
        // Admins must provide the business id from the admin dashboard.
        var businessId = ResolveBusinessId(model.BusinessId);
        if (businessId is null)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = User.IsInRole(PlatformRoles.Admin)
                ? "Please open Edit Business from the Admin Dashboard so the business can be identified."
                : "Your business could not be identified. Please sign in again.";

            return User.IsInRole(PlatformRoles.Admin)
                ? RedirectToAction("Index", "AdminDashboard")
                : RedirectToAction("Index", "BusinessDashboard");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.EditBusinessId = businessId.Value;
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "The business could not be saved. Please correct the highlighted fields and try again.";
            return View(model);
        }
        var business = await db.Businesses.FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();

        var ownerUser = await db.PlatformUsers.FirstOrDefaultAsync(
            x => x.BusinessId == business.Id && x.Role == PlatformRoles.BusinessOwner,
            cancellationToken);

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        if (!string.Equals(business.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var emailInUse = await db.PlatformUsers.AnyAsync(
                x => x.Email == normalizedEmail && (ownerUser == null || x.Id != ownerUser.Id),
                cancellationToken);

            if (emailInUse)
            {
                ModelState.AddModelError(nameof(model.Email), "This email address is already used by another platform user.");
                return View(model);
            }
        }

        var changedFields = new List<string>();
        void Track(string field, string oldValue, string newValue)
        {
            if (!string.Equals(oldValue ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
                changedFields.Add(field);
        }

        var name = model.Name.Trim();
        var businessType = model.BusinessType.Trim();
        var ownerName = model.OwnerName.Trim();
        var phone = model.PhoneNumber.Trim();
        var whatsapp = model.WhatsAppNumber.Trim();
        var address = model.Address.Trim();
        var city = model.City.Trim();
        var state = model.State.Trim();
        var postalCode = model.PostalCode.Trim();
        var country = string.IsNullOrWhiteSpace(model.Country) ? "India" : model.Country.Trim();
        var businessHours = model.BusinessHours.Trim();
        var tagline = model.Tagline.Trim();
        var description = model.Description.Trim();
        var logoUrl = model.LogoUrl.Trim();
        var heroImageUrl = model.HeroImageUrl.Trim();

        Track("Business name", business.Name, name);
        Track("Business type", business.BusinessType, businessType);
        Track("Owner name", business.OwnerName, ownerName);
        Track("Email", business.Email, normalizedEmail);
        Track("Phone number", business.PhoneNumber, phone);
        Track("WhatsApp number", business.WhatsAppNumber, whatsapp);
        Track("Address", business.Address, address);
        Track("City", business.City, city);
        Track("State", business.State, state);
        Track("Postal code", business.PostalCode, postalCode);
        Track("Country", business.Country, country);
        Track("Business hours", business.BusinessHours, businessHours);
        Track("Tagline", business.Tagline, tagline);
        Track("Description", business.Description, description);
        Track("Logo URL", business.LogoUrl, logoUrl);
        Track("Hero image URL", business.HeroImageUrl, heroImageUrl);

        business.Name = name;
        business.BusinessType = businessType;
        business.OwnerName = ownerName;
        business.Email = normalizedEmail;
        business.PhoneNumber = phone;
        business.WhatsAppNumber = whatsapp;
        business.Address = address;
        business.City = city;
        business.State = state;
        business.PostalCode = postalCode;
        business.Country = country;
        business.BusinessHours = businessHours;
        business.Tagline = tagline;
        business.Description = description;
        business.LogoUrl = logoUrl;
        business.HeroImageUrl = heroImageUrl;

        if (ownerUser is not null)
        {
            ownerUser.FullName = ownerName;
            ownerUser.Email = normalizedEmail;
            ownerUser.PhoneNumber = phone;
        }

        logger.LogInformation(
            "Saving business profile. BusinessId={BusinessId}, FieldsChanged={FieldsChanged}",
            business.Id, string.Join(", ", changedFields));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // Verify the saved values from a fresh database query. This catches a
        // persistence/configuration problem before we tell the user that the
        // save succeeded.
        var persisted = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == business.Id, cancellationToken);

        if (persisted is null)
        {
            logger.LogError("Business disappeared immediately after save. BusinessId={BusinessId}", business.Id);
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = $"{business.Name} could not be verified after saving. Please try again.";
            return RedirectToAction(nameof(Edit), new { id = business.Id });
        }

        if (ownerUser is not null)
        {
            var persistedOwner = await db.PlatformUsers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ownerUser.Id, cancellationToken);
            if (persistedOwner is null || !string.Equals(persistedOwner.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(persistedOwner.PhoneNumber, phone, StringComparison.Ordinal))
            {
                logger.LogError("Business owner account could not be verified after business save. BusinessId={BusinessId}, UserId={UserId}", business.Id, ownerUser.Id);
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = $"{business.Name} was not fully verified after saving. Please try again.";
                return RedirectToAction(nameof(Edit), new { id = business.Id });
            }
        }

        await transaction.CommitAsync(cancellationToken);

        businessCache.InvalidateBusiness(business.Id);
        await auditLogService.WriteAsync(
            AuditActions.BusinessProfileChanged, "Business", business.Id.ToString(), business.Id,
            System.Text.Json.JsonSerializer.Serialize(new { fieldsChanged = changedFields }), cancellationToken);

        logger.LogInformation(
            "Business profile saved successfully. BusinessId={BusinessId}", business.Id);

        var changeText = changedFields.Count == 0
            ? $"No changes were made to {business.Name}."
            : $"{business.Name} saved. Updated: {string.Join(", ", changedFields)}.";
        TempData["ToastType"] = changedFields.Count == 0 ? "info" : "success";
        TempData["ToastMessage"] = changeText;

        return User.IsInRole(PlatformRoles.Admin)
            ? RedirectToAction(nameof(Edit), new { id = business.Id })
            : RedirectToAction("Index", "BusinessDashboard");
    }

    [HttpGet]
    public async Task<IActionResult> WebsiteContent(int? id, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(id);
        if (businessId is null) return Forbid();

        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();

        var configuration = await db.WebsiteConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == businessId.Value, cancellationToken);

        if (configuration is null)
        {
            configuration = new WebsiteConfiguration
            {
                BusinessId = business.Id,
                WebsiteTitle = business.Name,
                HeroTitle = business.Name,
                HeroSubtitle = business.Description,
                AboutTitle = $"About {business.Name}",
                AboutContent = business.Description
            };
        }

        ViewBag.Business = business;
        ViewBag.Brochures = await brochureService.GetAsync(business.Id, cancellationToken);

        return View(new WebsiteContentViewModel
        {
            BusinessId = business.Id,
            WebsiteTitle = configuration.WebsiteTitle,
            MetaDescription = configuration.MetaDescription,
            HeroTitle = configuration.HeroTitle,
            HeroSubtitle = configuration.HeroSubtitle,
            AboutTitle = configuration.AboutTitle,
            AboutContent = configuration.AboutContent,
            WhyChooseUsTitle = configuration.WhyChooseUsTitle,
            WhyChooseUsContent = configuration.WhyChooseUsContent,
            ServicesTitle = configuration.ServicesTitle,
            ServicesContent = configuration.ServicesContent,
            CallToActionTitle = configuration.CallToActionTitle,
            CallToActionText = configuration.CallToActionText,
            ContactIntro = configuration.ContactIntro,
            FooterText = configuration.FooterText,
            ShowWhyChooseUs = configuration.ShowWhyChooseUs,
            ShowServices = configuration.ShowServices,
            ShowCallToAction = configuration.ShowCallToAction,
            ShowOffers = configuration.ShowOffers,
            ShowTestimonials = configuration.ShowTestimonials,
            ShowCatalog = configuration.ShowCatalog,
            ShowWhatsApp = configuration.ShowWhatsApp
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WebsiteContent(WebsiteContentViewModel model, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(model.BusinessId);
        if (businessId is null) return Forbid();
        if (!ModelState.IsValid)
        {
            ViewBag.Business = await db.Businesses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
            ViewBag.Brochures = await brochureService.GetAsync(businessId.Value, cancellationToken);
            return View(model);
        }

        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();

        var configuration = await db.WebsiteConfigurations
            .FirstOrDefaultAsync(x => x.BusinessId == businessId.Value, cancellationToken);

        if (configuration is null)
        {
            configuration = new WebsiteConfiguration { BusinessId = businessId.Value };
            db.WebsiteConfigurations.Add(configuration);
        }

        var changedFields = new List<string>();
        void Track(string field, string oldValue, string newValue)
        {
            if (!string.Equals(oldValue ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
                changedFields.Add(field);
        }

        var websiteTitle = model.WebsiteTitle.Trim();
        var metaDescription = model.MetaDescription.Trim();
        var heroTitle = model.HeroTitle.Trim();
        var heroSubtitle = model.HeroSubtitle.Trim();
        var aboutTitle = model.AboutTitle.Trim();
        var aboutContent = model.AboutContent.Trim();
        var whyTitle = model.WhyChooseUsTitle.Trim();
        var whyContent = model.WhyChooseUsContent.Trim();
        var servicesTitle = model.ServicesTitle.Trim();
        var servicesContent = model.ServicesContent.Trim();
        var callTitle = model.CallToActionTitle.Trim();
        var callText = model.CallToActionText.Trim();
        var contactIntro = model.ContactIntro.Trim();
        var footerText = model.FooterText.Trim();

        Track("Website title", configuration.WebsiteTitle, websiteTitle);
        Track("Meta description", configuration.MetaDescription, metaDescription);
        Track("Hero title", configuration.HeroTitle, heroTitle);
        Track("Hero subtitle", configuration.HeroSubtitle, heroSubtitle);
        Track("About title", configuration.AboutTitle, aboutTitle);
        Track("About content", configuration.AboutContent, aboutContent);
        Track("Why choose us title", configuration.WhyChooseUsTitle, whyTitle);
        Track("Why choose us content", configuration.WhyChooseUsContent, whyContent);
        Track("Services title", configuration.ServicesTitle, servicesTitle);
        Track("Services content", configuration.ServicesContent, servicesContent);
        Track("Call to action title", configuration.CallToActionTitle, callTitle);
        Track("Call to action text", configuration.CallToActionText, callText);
        Track("Contact introduction", configuration.ContactIntro, contactIntro);
        Track("Footer text", configuration.FooterText, footerText);
        if (configuration.ShowWhyChooseUs != model.ShowWhyChooseUs) changedFields.Add("Show Why Choose Us");
        if (configuration.ShowServices != model.ShowServices) changedFields.Add("Show Services");
        if (configuration.ShowCallToAction != model.ShowCallToAction) changedFields.Add("Show Call To Action");
        if (configuration.ShowOffers != model.ShowOffers) changedFields.Add("Show Offers");
        if (configuration.ShowTestimonials != model.ShowTestimonials) changedFields.Add("Show Testimonials");
        if (configuration.ShowCatalog != model.ShowCatalog) changedFields.Add("Show Catalog");
        if (configuration.ShowWhatsApp != model.ShowWhatsApp) changedFields.Add("Show WhatsApp");

        configuration.WebsiteTitle = websiteTitle;
        configuration.MetaDescription = metaDescription;
        configuration.HeroTitle = heroTitle;
        configuration.HeroSubtitle = heroSubtitle;
        configuration.AboutTitle = aboutTitle;
        configuration.AboutContent = aboutContent;
        configuration.WhyChooseUsTitle = whyTitle;
        configuration.WhyChooseUsContent = whyContent;
        configuration.ServicesTitle = servicesTitle;
        configuration.ServicesContent = servicesContent;
        configuration.CallToActionTitle = callTitle;
        configuration.CallToActionText = callText;
        configuration.ContactIntro = contactIntro;
        configuration.FooterText = footerText;
        configuration.ShowWhyChooseUs = model.ShowWhyChooseUs;
        configuration.ShowServices = model.ShowServices;
        configuration.ShowCallToAction = model.ShowCallToAction;
        configuration.ShowOffers = model.ShowOffers;
        configuration.ShowTestimonials = model.ShowTestimonials;
        configuration.ShowCatalog = model.ShowCatalog;
        configuration.ShowWhatsApp = model.ShowWhatsApp;

        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(businessId.Value);
        await auditLogService.WriteAsync(
            AuditActions.WebsiteContentChanged, "WebsiteConfiguration", configuration.Id.ToString(), businessId.Value,
            System.Text.Json.JsonSerializer.Serialize(new { fieldsChanged = changedFields }), cancellationToken);
        TempData["ToastType"] = changedFields.Count == 0 ? "info" : "success";
        TempData["ToastMessage"] = changedFields.Count == 0
            ? $"No website content changes were made for {business.Name}."
            : $"Website content for {business.Name} was saved. Updated: {string.Join(", ", changedFields)}.";
        TempData["WebsiteContentSuccess"] = "Website content saved successfully.";
        return RedirectToAction(nameof(WebsiteContent), new { id = businessId.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadBrochure(int businessId, IFormFile brochure, CancellationToken cancellationToken)
    {
        var resolvedId = ResolveBusinessId(businessId);
        if (resolvedId is null) return Forbid();
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == resolvedId.Value, cancellationToken);
        if (business is null) return NotFound();

        try
        {
            var saved = await brochureService.SaveAsync(resolvedId.Value, brochure, cancellationToken);
            businessCache.InvalidateBusiness(resolvedId.Value);
            await auditLogService.WriteAsync(AuditActions.WebsiteContentChanged, "BusinessBrochure", saved.FileName, resolvedId.Value,
                System.Text.Json.JsonSerializer.Serialize(new { action = "uploaded", saved.DisplayName, saved.Length }), cancellationToken);
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"Brochure '{saved.DisplayName}' was uploaded successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(WebsiteContent), new { id = resolvedId.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBrochure(int businessId, string fileName, CancellationToken cancellationToken)
    {
        var resolvedId = ResolveBusinessId(businessId);
        if (resolvedId is null) return Forbid();
        var deleted = await brochureService.DeleteAsync(resolvedId.Value, fileName, cancellationToken);
        if (deleted)
        {
            businessCache.InvalidateBusiness(resolvedId.Value);
            await auditLogService.WriteAsync(AuditActions.WebsiteContentChanged, "BusinessBrochure", Path.GetFileName(fileName), resolvedId.Value,
                "{\"action\":\"deleted\"}", cancellationToken);
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Brochure was removed.";
        }
        else
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "The brochure could not be found.";
        }
        return RedirectToAction(nameof(WebsiteContent), new { id = resolvedId.Value });
    }

    private int? ResolveBusinessId(int? requestedId)
    {
        if (User.IsInRole(PlatformRoles.Admin) && requestedId.HasValue)
            return requestedId;
        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

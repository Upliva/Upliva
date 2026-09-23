using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Controllers;

[Authorize(Roles = "Admin,BusinessOwner")]
public class BusinessManagementController(UplivaDbContext db) : Controller
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
        business.State = model.State.Trim();
        business.PostalCode = model.PostalCode.Trim();
        business.Country = string.IsNullOrWhiteSpace(model.Country) ? "India" : model.Country.Trim();
        business.BusinessHours = model.BusinessHours.Trim();
        business.Tagline = model.Tagline.Trim();
        business.Description = model.Description.Trim();
        business.LogoUrl = model.LogoUrl.Trim();
        business.HeroImageUrl = model.HeroImageUrl.Trim();

        await db.SaveChangesAsync(cancellationToken);
        return User.IsInRole(PlatformRoles.Admin)
            ? RedirectToAction("Index", "AdminDashboard")
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
            ShowCallToAction = configuration.ShowCallToAction
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
            return View(model);
        }

        var configuration = await db.WebsiteConfigurations
            .FirstOrDefaultAsync(x => x.BusinessId == businessId.Value, cancellationToken);

        if (configuration is null)
        {
            configuration = new WebsiteConfiguration { BusinessId = businessId.Value };
            db.WebsiteConfigurations.Add(configuration);
        }

        configuration.WebsiteTitle = model.WebsiteTitle.Trim();
        configuration.MetaDescription = model.MetaDescription.Trim();
        configuration.HeroTitle = model.HeroTitle.Trim();
        configuration.HeroSubtitle = model.HeroSubtitle.Trim();
        configuration.AboutTitle = model.AboutTitle.Trim();
        configuration.AboutContent = model.AboutContent.Trim();
        configuration.WhyChooseUsTitle = model.WhyChooseUsTitle.Trim();
        configuration.WhyChooseUsContent = model.WhyChooseUsContent.Trim();
        configuration.ServicesTitle = model.ServicesTitle.Trim();
        configuration.ServicesContent = model.ServicesContent.Trim();
        configuration.CallToActionTitle = model.CallToActionTitle.Trim();
        configuration.CallToActionText = model.CallToActionText.Trim();
        configuration.ContactIntro = model.ContactIntro.Trim();
        configuration.FooterText = model.FooterText.Trim();
        configuration.ShowWhyChooseUs = model.ShowWhyChooseUs;
        configuration.ShowServices = model.ShowServices;
        configuration.ShowCallToAction = model.ShowCallToAction;

        await db.SaveChangesAsync(cancellationToken);
        TempData["WebsiteContentSuccess"] = "Website content saved successfully.";
        return RedirectToAction(nameof(WebsiteContent), new { id = businessId.Value });
    }

    private int? ResolveBusinessId(int? requestedId)
    {
        if (User.IsInRole(PlatformRoles.Admin) && requestedId.HasValue)
            return requestedId;
        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

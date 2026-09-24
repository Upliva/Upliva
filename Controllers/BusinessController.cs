using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Route("business")]
public class BusinessController(
    IBusinessService businessService,
    UplivaDbContext db,
    IBusinessCacheService businessCache,
    IBusinessBrochureService brochureService) : Controller
{
    [HttpGet("{slug}")]
    public async Task<IActionResult> Website(string slug, CancellationToken cancellationToken)
    {
        var business = await businessService.GetBySlugAsync(slug, cancellationToken);
        if (business is null)
            return NotFound();

        // If the business has chosen and verified its own domain, make that domain
        // the canonical public URL. The UplivaAI /business/{slug} URL remains a
        // fallback entry point, but customers are redirected to the business domain.
        if (business.IsCustomDomainEnabled && business.IsCustomDomainVerified &&
            !string.IsNullOrWhiteSpace(business.CustomDomain))
        {
            var requestHost = Request.Host.Host.Trim().ToLowerInvariant();
            var customHost = business.CustomDomain.Trim().ToLowerInvariant();
            if (!string.Equals(requestHost, customHost, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectPermanent($"https://{customHost}/");
            }
        }

        var model = await businessCache.GetOrCreateWebsiteAsync(
            business.Id,
            () => BuildWebsiteModelAsync(business, cancellationToken),
            cancellationToken);

        return View("Website", model);
    }

    [HttpPost("{slug}/enquiry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enquiry(string slug, BusinessEnquiryViewModel model, CancellationToken cancellationToken)
    {
        var business = await businessService.GetBySlugAsync(slug, cancellationToken);
        if (business is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            model.Slug = slug;
            return View("Website", await BuildWebsiteModelAsync(business, cancellationToken));
        }

        db.BusinessEnquiries.Add(new BusinessEnquiry
        {
            BusinessId = business.Id,
            Name = model.Name.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Email = model.Email.Trim(),
            Message = model.Message.Trim(),
            Status = "New",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        TempData["EnquirySuccess"] = "Thanks! Your enquiry has been sent to the business team.";
        return RedirectToAction(nameof(Website), new { slug });
    }

    private async Task<BusinessWebsiteViewModel> BuildWebsiteModelAsync(
        Business business,
        CancellationToken cancellationToken)
    {
        var configuration = await db.WebsiteConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == business.Id, cancellationToken)
            ?? new WebsiteConfiguration { BusinessId = business.Id };

        // The public business website shows the full active catalog. WhatsApp uses only Admin-selected featured products.
        var catalog = await db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var topCatalog = catalog
            .Where(x => x.IsWhatsAppTopPick)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToList();

        return new BusinessWebsiteViewModel
        {
            Business = business,
            Configuration = configuration,
            Catalog = catalog,
            TopCatalog = topCatalog,
            Offers = await db.BusinessOffers.AsNoTracking()
                .Where(x => x.BusinessId == business.Id && x.IsPublished)
                .OrderByDescending(x => x.Id)
                .ToListAsync(cancellationToken),
            Testimonials = await db.BusinessTestimonials.AsNoTracking()
                .Where(x => x.BusinessId == business.Id && x.IsPublished && !x.IsDemo)
                .ToListAsync(cancellationToken),
            Brochures = await brochureService.GetAsync(business.Id, cancellationToken)
        };
    }
}

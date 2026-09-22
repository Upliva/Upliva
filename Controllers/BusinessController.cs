using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;
using UplivaResortBooking.Models;
using UplivaResortBooking.Services;

namespace UplivaResortBooking.Controllers;

[Route("business")]
public class BusinessController(IBusinessService businessService, ResortDbContext db) : Controller
{
    [HttpGet("{slug}")]
    public async Task<IActionResult> Website(string slug, CancellationToken cancellationToken)
    {
        var business = await businessService.GetBySlugAsync(slug, cancellationToken);
        if (business is null)
            return NotFound();

        var configuration = await db.WebsiteConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == business.Id, cancellationToken)
            ?? new WebsiteConfiguration { BusinessId = business.Id };

        var model = new BusinessWebsiteViewModel
        {
            Business = business,
            Configuration = configuration,
            Catalog = await db.BusinessCatalogItems.AsNoTracking()
                .Where(x => x.BusinessId == business.Id && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken),
            Offers = await db.BusinessOffers.AsNoTracking()
                .Where(x => x.BusinessId == business.Id && x.IsPublished)
                .OrderByDescending(x => x.Id)
                .ToListAsync(cancellationToken),
            Testimonials = await db.BusinessTestimonials.AsNoTracking()
                .Where(x => x.BusinessId == business.Id && x.IsPublished && !x.IsDemo)
                .ToListAsync(cancellationToken)
        };

        // Only approved + published businesses are returned by BusinessService.
        // The public route is therefore the shareable URL for a real business after admin publishing.
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
            return View("Website", new BusinessWebsiteViewModel
            {
                Business = business,
                Configuration = await db.WebsiteConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.BusinessId == business.Id, cancellationToken)
                    ?? new WebsiteConfiguration { BusinessId = business.Id },
                Catalog = await db.BusinessCatalogItems.AsNoTracking().Where(x => x.BusinessId == business.Id && x.IsActive).ToListAsync(cancellationToken),
                Offers = await db.BusinessOffers.AsNoTracking().Where(x => x.BusinessId == business.Id && x.IsPublished).ToListAsync(cancellationToken),
                Testimonials = await db.BusinessTestimonials.AsNoTracking().Where(x => x.BusinessId == business.Id && x.IsPublished && !x.IsDemo).ToListAsync(cancellationToken)
            });
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
}

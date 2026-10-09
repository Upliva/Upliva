using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;
using UplivaAI.Middleware;

namespace UplivaAI.Controllers;

public class PublicBusinessController(
    UplivaDbContext db,
    INotificationService notificationService,
    IIntegrationLogService integrationLogService,
    IConfiguration configuration,
    ILogger<PublicBusinessController> logger) : Controller
{
    [HttpGet("/business/{slug}")]
    public async Task<IActionResult> Catalog(string slug, string? search, string? category, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.Status == BusinessStatuses.Approved, cancellationToken);
        if (business is null) return NotFound();

        var q = search?.Trim() ?? string.Empty;
        var cat = category?.Trim() ?? string.Empty;

        var baseQuery = db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive);

        if (!string.IsNullOrWhiteSpace(cat))
            baseQuery = baseQuery.Where(x => x.Category == cat);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q}%";
            baseQuery = baseQuery.Where(x =>
                EF.Functions.Like(x.Name, pattern) ||
                EF.Functions.Like(x.Category, pattern) ||
                EF.Functions.Like(x.Description, pattern) ||
                EF.Functions.Like(x.ShortDescription, pattern));
        }

        var products = await baseQuery
            .OrderBy(x => x.Category).ThenBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var categories = await db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive && x.Category != "")
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var today = DateTime.UtcNow;
        var offers = await db.BusinessOffers.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive &&
                        (!x.StartsOn.HasValue || x.StartsOn.Value <= today) &&
                        (!x.EndsOn.HasValue || x.EndsOn.Value >= today))
            .OrderBy(x => x.EndsOn)
            .Take(20)
            .ToListAsync(cancellationToken);

        return View(new PublicCatalogViewModel
        {
            Business = business,
            Products = products,
            Search = q,
            Category = cat,
            Categories = categories,
            Offers = offers
        });
    }

    [HttpPost("/business/{slug}/call")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("call-click")]
    public async Task<IActionResult> Call(string slug, int? catalogItemId, string? source, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString() ?? HttpContext.TraceIdentifier;
        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.Status == BusinessStatuses.Approved, cancellationToken);
        if (business is null) return NotFound();

        if (catalogItemId.HasValue && !await db.BusinessCatalogItems.AnyAsync(x => x.Id == catalogItemId && x.BusinessId == business.Id && x.IsActive, cancellationToken))
            catalogItemId = null;

        db.CallEvents.Add(new CallEvent
        {
            BusinessId = business.Id,
            CatalogItemId = catalogItemId,
            Source = string.IsNullOrWhiteSpace(source) ? "PublicBusinessPage" : Truncate(source.Trim(), 80),
            CorrelationId = Truncate(correlationId, 100),
            VisitorHash = BuildVisitorHash(HttpContext, configuration["Security:VisitorHashSalt"]),
            UserAgent = Truncate(Request.Headers.UserAgent.ToString(), 200),
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        await integrationLogService.WriteAsync(business.Id, "Upliva", "CallClick", "Success", correlationId, stopwatch.ElapsedMilliseconds, cancellationToken: cancellationToken);

        var sourcePhone = string.IsNullOrWhiteSpace(business.PhoneNumber) ? business.WhatsAppNumber : business.PhoneNumber;
        var phone = new string((sourcePhone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(phone))
            return RedirectToAction(nameof(Catalog), new { slug });

        return Redirect("tel:+" + phone);
    }

    [HttpPost("/business/{slug}/enquiry")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-enquiry")]
    public async Task<IActionResult> Enquiry(string slug, PublicEnquiryViewModel model, CancellationToken cancellationToken)
    {
        model.Slug = slug;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var correlationId = HttpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString() ?? HttpContext.TraceIdentifier;

        var business = await db.Businesses.FirstOrDefaultAsync(x => x.Slug == slug && x.Status == BusinessStatuses.Approved, cancellationToken);
        if (business is null) return NotFound();

        model.Name = model.Name?.Trim() ?? string.Empty;
        model.PhoneNumber = new string((model.PhoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        model.Email = model.Email?.Trim() ?? string.Empty;
        model.Message = model.Message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Please enter your name.");
        if (model.PhoneNumber.Length < 10 || model.PhoneNumber.Length > 15)
            ModelState.AddModelError(nameof(model.PhoneNumber), "Enter a valid mobile number.");
        if (model.Message.Length < 3)
            ModelState.AddModelError(nameof(model.Message), "Please enter a short message.");

        if (model.CatalogItemId.HasValue && !await db.BusinessCatalogItems.AnyAsync(
                x => x.Id == model.CatalogItemId.Value && x.BusinessId == business.Id && x.IsActive,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(model.CatalogItemId), "The selected product is no longer available. Please select the product again.");
        }

        if (!ModelState.IsValid)
        {
            TempData["PublicEnquiryError"] = "We couldn't submit your enquiry. Please check your name, phone number, email and message, then try again.";
            return RedirectToAction(nameof(Catalog), new { slug });
        }

        var enquiry = new BusinessEnquiry
        {
            BusinessId = business.Id,
            CatalogItemId = model.CatalogItemId,
            Name = model.Name,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            Message = model.Message,
            Status = "New",
            Source = "PublicCatalog",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.BusinessEnquiries.Add(enquiry);
        await db.SaveChangesAsync(cancellationToken); // Lead is durable before notification.

        try
        {
            await notificationService.QueueBusinessEnquiryEmailAsync(enquiry, business, cancellationToken);
        }
        catch (Exception ex)
        {
            // The enquiry is already durable. Notification infrastructure must never turn a saved lead into a customer-facing 500.
            logger.LogError(ex, "Could not queue owner notification. BusinessId={BusinessId}, EnquiryId={EnquiryId}, CorrelationId={CorrelationId}", business.Id, enquiry.Id, correlationId);
        }
        stopwatch.Stop();
        await integrationLogService.WriteAsync(business.Id, "Upliva", "BusinessEnquiry", "Success", correlationId, stopwatch.ElapsedMilliseconds, cancellationToken: cancellationToken);

        TempData["PublicEnquirySuccess"] = "Thank you! Your enquiry has been submitted successfully. The business can now review your request and contact you.";
        return RedirectToAction(nameof(Catalog), new { slug });
    }

    private static string BuildVisitorHash(HttpContext context, string? salt)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var value = (salt ?? "upliva-development-only") + "|" + ip;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}

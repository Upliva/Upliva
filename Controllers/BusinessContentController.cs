using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Authorize(Roles = "Admin,BusinessOwner")]
public class BusinessContentController(
    UplivaDbContext db,
    IAuditLogService auditLogService,
    IBusinessCacheService businessCache) : Controller
{

    [HttpGet]
    public async Task<IActionResult> Index(int? id, string? category, string? search, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(id);
        if (businessId is null) return Forbid();

        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();
        if (!await AdminCanManageCatalogAsync(business.Id, cancellationToken)) return Forbid();

        await LoadContentAsync(business.Id, category, search, cancellationToken);

        return View(new BusinessCatalogItemViewModel { BusinessId = business.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCatalog(BusinessCatalogItemViewModel itemModel, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(itemModel.BusinessId);
        if (businessId is null) return Forbid();

        var businessExists = await db.Businesses.AnyAsync(x => x.Id == businessId.Value, cancellationToken);
        if (!businessExists) return NotFound();
        if (!await AdminCanManageCatalogAsync(businessId.Value, cancellationToken)) return Forbid();

        if (!string.IsNullOrWhiteSpace(itemModel.SKU))
        {
            var duplicateSku = await db.BusinessCatalogItems.AnyAsync(
                x => x.BusinessId == businessId.Value && x.SKU == itemModel.SKU.Trim() && x.IsActive,
                cancellationToken);

            if (duplicateSku)
            {
                ModelState.AddModelError(nameof(itemModel.SKU), "This SKU is already used by another active product in this business.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadContentAsync(businessId.Value, null, null, cancellationToken);
            itemModel.BusinessId = businessId.Value;
            return View(nameof(Index), itemModel);
        }

        var item = new BusinessCatalogItem
        {
            BusinessId = businessId.Value,
            Name = itemModel.Name.Trim(),
            Brand = itemModel.Brand.Trim(),
            Model = itemModel.Model.Trim(),
            SKU = itemModel.SKU.Trim(),
            Category = itemModel.Category.Trim(),
            PriceText = itemModel.PriceText.Trim(),
            OriginalPriceText = itemModel.OriginalPriceText.Trim(),
            DiscountText = itemModel.DiscountText.Trim(),
            ImageUrl = itemModel.ImageUrl.Trim(),
            ShortDescription = itemModel.ShortDescription.Trim(),
            Description = itemModel.Description.Trim(),
            StockStatus = itemModel.StockStatus.Trim(),
            Rating = itemModel.Rating,
            ReviewCount = itemModel.ReviewCount,
            IsWhatsAppTopPick = false,
            ShowOnWebsite = itemModel.ShowOnWebsite,
            IsActive = true,
            SortOrder = itemModel.SortOrder
        };

        db.BusinessCatalogItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(businessId.Value);
        await auditLogService.WriteAsync(
            AuditActions.CatalogAdded, "BusinessCatalogItem", item.Id.ToString(), businessId.Value,
            $"{{\"name\":{System.Text.Json.JsonSerializer.Serialize(item.Name)},\"topPick\":{item.IsWhatsAppTopPick.ToString().ToLowerInvariant()}}}", cancellationToken);

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"{item.Name} was added to the catalog. Admin can select it for WhatsApp featured.";

        return RedirectToAction(nameof(Index), new { id = businessId });
    }

    [HttpGet]
    public async Task<IActionResult> PreviewWhatsApp(int id, int? productId, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(id);
        if (businessId is null) return Forbid();

        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);

        if (business is null) return NotFound();
        if (!await AdminCanManageCatalogAsync(business.Id, cancellationToken)) return Forbid();

        var catalog = await db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive)
            .OrderBy(x => x.IsWhatsAppTopPick ? 0 : 1)
            .ThenBy(x => x.IsWhatsAppTopPick ? x.SortOrder : int.MaxValue)
            .ThenBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var products = catalog
            .Where(x => x.IsWhatsAppTopPick)
            .OrderBy(x => x.SortOrder <= 0 ? int.MaxValue : x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(MapWhatsAppPreviewProduct)
            .ToList();

        // If the admin clicks Preview on a catalog row that is not yet selected
        // for WhatsApp, show that product in the preview as a focused card.
        WhatsAppPreviewProductViewModel? focusProduct = null;
        if (productId.HasValue)
        {
            var focus = catalog.FirstOrDefault(x => x.Id == productId.Value);
            if (focus is null) return NotFound();
            focusProduct = MapWhatsAppPreviewProduct(focus);
        }

        var model = new WhatsAppPreviewViewModel
        {
            BusinessId = business.Id,
            BusinessName = business.Name,
            BusinessType = business.BusinessType,
            WhatsAppNumber = business.WhatsAppNumber,
            ServicePlan = business.ServicePlan,
            Tagline = business.Tagline,
            LogoUrl = business.LogoUrl,
            Products = products,
            FocusProduct = focusProduct
        };

        return View(model);
    }

    private static WhatsAppPreviewProductViewModel MapWhatsAppPreviewProduct(BusinessCatalogItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Category = item.Category,
        PriceText = item.PriceText,
        OriginalPriceText = item.OriginalPriceText,
        DiscountText = item.DiscountText,
        ImageUrl = item.ImageUrl,
        ShortDescription = item.ShortDescription,
        Description = item.Description,
        StockStatus = item.StockStatus,
        Rating = item.Rating,
        ReviewCount = item.ReviewCount,
        IsSelectedForWhatsApp = item.IsWhatsAppTopPick,
        Rank = item.IsWhatsAppTopPick ? item.SortOrder : 0
    };

    [HttpGet]
    public async Task<IActionResult> EditCatalog(int id, CancellationToken cancellationToken)
    {
        var item = await db.BusinessCatalogItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null) return NotFound();

        var businessId = ResolveBusinessId(item.BusinessId);
        if (businessId is null) return Forbid();
        if (!await AdminCanManageCatalogAsync(businessId.Value, cancellationToken)) return Forbid();

        await LoadContentAsync(businessId.Value, null, null, cancellationToken);
        ViewBag.IsEditingCatalog = true;

        return View(nameof(Index), new BusinessCatalogItemViewModel
        {
            Id = item.Id,
            BusinessId = item.BusinessId,
            Name = item.Name,
            Brand = item.Brand,
            Model = item.Model,
            SKU = item.SKU,
            Category = item.Category,
            PriceText = item.PriceText,
            OriginalPriceText = item.OriginalPriceText,
            DiscountText = item.DiscountText,
            ImageUrl = item.ImageUrl,
            ShortDescription = item.ShortDescription,
            Description = item.Description,
            StockStatus = item.StockStatus,
            Rating = item.Rating,
            ReviewCount = item.ReviewCount,
            IsWhatsAppTopPick = item.IsWhatsAppTopPick,
            ShowOnWebsite = item.ShowOnWebsite,
            SortOrder = item.SortOrder
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCatalog(BusinessCatalogItemViewModel itemModel, CancellationToken cancellationToken)
    {
        var existing = await db.BusinessCatalogItems
            .FirstOrDefaultAsync(x => x.Id == itemModel.Id, cancellationToken);

        if (existing is null) return NotFound();

        var businessId = ResolveBusinessId(existing.BusinessId);
        if (businessId is null) return Forbid();
        if (!await AdminCanManageCatalogAsync(businessId.Value, cancellationToken)) return Forbid();

        itemModel.BusinessId = businessId.Value;

        if (!string.IsNullOrWhiteSpace(itemModel.SKU))
        {
            var normalizedSku = itemModel.SKU.Trim();
            var duplicateSku = await db.BusinessCatalogItems.AnyAsync(
                x => x.BusinessId == businessId.Value
                    && x.Id != existing.Id
                    && x.SKU == normalizedSku
                    && x.IsActive,
                cancellationToken);

            if (duplicateSku)
            {
                ModelState.AddModelError(nameof(itemModel.SKU),
                    "This SKU is already used by another active product in this business.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadContentAsync(businessId.Value, null, null, cancellationToken);
            ViewBag.IsEditingCatalog = true;
            return View(nameof(Index), itemModel);
        }

        existing.Name = itemModel.Name.Trim();
        existing.Brand = itemModel.Brand.Trim();
        existing.Model = itemModel.Model.Trim();
        existing.SKU = itemModel.SKU.Trim();
        existing.Category = itemModel.Category.Trim();
        existing.PriceText = itemModel.PriceText.Trim();
        existing.OriginalPriceText = itemModel.OriginalPriceText.Trim();
        existing.DiscountText = itemModel.DiscountText.Trim();
        existing.ImageUrl = itemModel.ImageUrl.Trim();
        existing.ShortDescription = itemModel.ShortDescription.Trim();
        existing.Description = itemModel.Description.Trim();
        existing.StockStatus = itemModel.StockStatus.Trim();
        existing.Rating = itemModel.Rating;
        existing.ReviewCount = itemModel.ReviewCount;
        // WhatsApp featured status is Admin-controlled; BusinessOwner edits do not change it.
        existing.ShowOnWebsite = itemModel.ShowOnWebsite;
        existing.SortOrder = itemModel.SortOrder;

        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(businessId.Value);
        await auditLogService.WriteAsync(
            AuditActions.CatalogUpdated, "BusinessCatalogItem", existing.Id.ToString(), businessId.Value,
            $"{{\"name\":{System.Text.Json.JsonSerializer.Serialize(existing.Name)},\"isWhatsAppFeatured\":{existing.IsWhatsAppTopPick}}}", cancellationToken);

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"{existing.Name} was updated successfully.";
        return RedirectToAction(nameof(Index), new { id = businessId.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCatalog(int id, CancellationToken cancellationToken)
    {
        var item = await db.BusinessCatalogItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();

        var businessId = ResolveBusinessId(item.BusinessId);
        if (businessId is null) return Forbid();
        if (!await AdminCanManageCatalogAsync(businessId.Value, cancellationToken)) return Forbid();

        db.BusinessCatalogItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(businessId.Value);
        await auditLogService.WriteAsync(
            AuditActions.CatalogDeleted, "BusinessCatalogItem", item.Id.ToString(), businessId.Value,
            $"{{\"name\":{System.Text.Json.JsonSerializer.Serialize(item.Name)}}}", cancellationToken);

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"{item.Name} was removed from the catalog.";
        return RedirectToAction(nameof(Index), new { id = businessId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOffer(BusinessOfferViewModel model, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(model.BusinessId);
        if (businessId is null) return Forbid();
        if (!ModelState.IsValid)
        {
            await LoadContentAsync(businessId.Value, null, null, cancellationToken);
            return View(nameof(Index), new BusinessCatalogItemViewModel { BusinessId = businessId.Value });
        }

        var offer = new BusinessOffer
        {
            BusinessId = businessId.Value,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            ImageUrl = model.ImageUrl.Trim(),
            DiscountText = model.DiscountText.Trim(),
            IsPublished = User.IsInRole(PlatformRoles.Admin)
        };

        db.BusinessOffers.Add(offer);
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(businessId.Value);
        await auditLogService.WriteAsync(
            AuditActions.OfferAdded, "BusinessOffer", offer.Id.ToString(), businessId.Value,
            $"{{\"title\":{System.Text.Json.JsonSerializer.Serialize(offer.Title)},\"published\":{offer.IsPublished}}}", cancellationToken);

        // Confirm the row was persisted before redirecting. This keeps the
        // existing admin-publication workflow unchanged while making a failed
        // persistence path visible instead of silently returning to the list.
        var saved = await db.BusinessOffers.AsNoTracking()
            .AnyAsync(x => x.Id == offer.Id && x.BusinessId == businessId.Value, cancellationToken);

        if (!saved)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = $"{offer.Title} could not be saved. Please try again.";
            return RedirectToAction(nameof(Index), new { id = businessId.Value });
        }

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = offer.IsPublished
            ? $"{offer.Title} was added and published successfully."
            : $"{offer.Title} was added successfully and is waiting for admin publication.";

        return RedirectToAction(nameof(Index), new { id = businessId.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishOffer(int id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(PlatformRoles.Admin)) return Forbid();
        var offer = await db.BusinessOffers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (offer is null) return NotFound();
        offer.IsPublished = true;
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(offer.BusinessId);
        await auditLogService.WriteAsync(
            AuditActions.OfferPublished, "BusinessOffer", offer.Id.ToString(), offer.BusinessId,
            "{\"isPublished\":true}", cancellationToken);
        return RedirectToAction(nameof(Index), new { id = offer.BusinessId });
    }

    private async Task LoadContentAsync(int businessId, string? category, string? search, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == businessId, cancellationToken);

        ViewBag.Business = business!;

        var allCatalog = await db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.IsActive)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        ViewBag.AllCatalogCount = allCatalog.Count;
        ViewBag.Catalog = allCatalog
            .Where(x => string.IsNullOrWhiteSpace(category) || string.Equals(x.Category, category.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(search)
                || x.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
                || x.Category.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
                || x.SKU.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        ViewBag.Categories = allCatalog
            .Select(x => x.Category?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        ViewBag.SelectedCategory = category ?? string.Empty;
        ViewBag.Search = search ?? string.Empty;
        ViewBag.TopPickCount = allCatalog.Count(x => x.IsWhatsAppTopPick);
        ViewBag.Offers = await db.BusinessOffers.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> AdminCanManageCatalogAsync(int businessId, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(PlatformRoles.Admin))
            return true;

        // An approved business is an operational business. Admin must be able to
        // manage its catalog even if the original lead is no longer present.
        var approved = await db.Businesses.AsNoTracking()
            .AnyAsync(x => x.Id == businessId && x.Status == BusinessStatuses.Approved, cancellationToken);

        if (approved) return true;

        return await db.ChatbotLeads.AnyAsync(
            x => x.ConvertedBusinessId == businessId && x.Status == MarketingLeadStatuses.Interested,
            cancellationToken);
    }

    private int? ResolveBusinessId(int? requestedId)
    {
        if (User.IsInRole(PlatformRoles.Admin) && requestedId.HasValue)
            return requestedId;

        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

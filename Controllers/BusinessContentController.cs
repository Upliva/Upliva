using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Controllers;

[Authorize(Roles = "Admin,BusinessOwner")]
public class BusinessContentController(UplivaDbContext db) : Controller
{
    private const int WhatsAppTopPickLimit = 6;

    [HttpGet]
    public async Task<IActionResult> Index(int? id, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(id);
        if (businessId is null) return Forbid();

        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();

        await LoadContentAsync(business.Id, cancellationToken);

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

        if (itemModel.IsWhatsAppTopPick)
        {
            var topPickCount = await db.BusinessCatalogItems.CountAsync(
                x => x.BusinessId == businessId.Value && x.IsActive && x.IsWhatsAppTopPick,
                cancellationToken);

            if (topPickCount >= WhatsAppTopPickLimit)
            {
                ModelState.AddModelError(nameof(itemModel.IsWhatsAppTopPick),
                    "Only 6 products can be selected for the WhatsApp Top 6 showcase.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadContentAsync(businessId.Value, cancellationToken);
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
            IsWhatsAppTopPick = itemModel.IsWhatsAppTopPick,
            ShowOnWebsite = itemModel.IsWhatsAppTopPick || itemModel.ShowOnWebsite,
            IsActive = true,
            SortOrder = itemModel.SortOrder
        };

        db.BusinessCatalogItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        TempData["CatalogSuccess"] = itemModel.IsWhatsAppTopPick
            ? "Product added and included in the WhatsApp Top 6 showcase."
            : "Product added to the catalog.";

        return RedirectToAction(nameof(Index), new { id = businessId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWhatsAppTopPick(int id, CancellationToken cancellationToken)
    {
        var item = await db.BusinessCatalogItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();

        var businessId = ResolveBusinessId(item.BusinessId);
        if (businessId is null) return Forbid();

        if (!item.IsWhatsAppTopPick)
        {
            var topPickCount = await db.BusinessCatalogItems.CountAsync(
                x => x.BusinessId == item.BusinessId && x.IsActive && x.IsWhatsAppTopPick,
                cancellationToken);

            if (topPickCount >= WhatsAppTopPickLimit)
            {
                TempData["CatalogError"] = "The WhatsApp showcase already has 6 products. Remove one before adding another.";
                return RedirectToAction(nameof(Index), new { id = businessId });
            }
        }

        item.IsWhatsAppTopPick = !item.IsWhatsAppTopPick;
        if (item.IsWhatsAppTopPick)
            item.ShowOnWebsite = true;

        await db.SaveChangesAsync(cancellationToken);

        TempData["CatalogSuccess"] = item.IsWhatsAppTopPick
            ? $"{item.Name} is now in the WhatsApp Top 6 showcase."
            : $"{item.Name} was removed from the WhatsApp Top 6 showcase.";

        return RedirectToAction(nameof(Index), new { id = businessId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCatalog(int id, CancellationToken cancellationToken)
    {
        var item = await db.BusinessCatalogItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();

        var businessId = ResolveBusinessId(item.BusinessId);
        if (businessId is null) return Forbid();

        db.BusinessCatalogItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);

        TempData["CatalogSuccess"] = "Product removed from the catalog.";
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
            await LoadContentAsync(businessId.Value, cancellationToken);
            return View(nameof(Index), new BusinessCatalogItemViewModel { BusinessId = businessId.Value });
        }

        db.BusinessOffers.Add(new BusinessOffer
        {
            BusinessId = businessId.Value,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            ImageUrl = model.ImageUrl.Trim(),
            DiscountText = model.DiscountText.Trim(),
            IsPublished = User.IsInRole(PlatformRoles.Admin)
        });
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { id = businessId });
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
        return RedirectToAction(nameof(Index), new { id = offer.BusinessId });
    }

    private async Task LoadContentAsync(int businessId, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == businessId, cancellationToken);

        ViewBag.Business = business!;
        ViewBag.Catalog = await db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        ViewBag.TopPickCount = await db.BusinessCatalogItems.CountAsync(
            x => x.BusinessId == businessId && x.IsActive && x.IsWhatsAppTopPick,
            cancellationToken);
        ViewBag.Offers = await db.BusinessOffers.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    private int? ResolveBusinessId(int? requestedId)
    {
        if (User.IsInRole(PlatformRoles.Admin) && requestedId.HasValue)
            return requestedId;

        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

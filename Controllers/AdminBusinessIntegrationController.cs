using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Authorize(Roles = PlatformRoles.Admin)]
public class AdminBusinessIntegrationController(
    UplivaDbContext db,
    IAuditLogService auditLogService,
    IWhatsAppService whatsAppService,
    IBusinessCacheService businessCache,
    ILogger<AdminBusinessIntegrationController> logger,
    IConfiguration configuration) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Manage(int id, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (business is null) return NotFound();
        if (business.Status != BusinessStatuses.Approved)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Only approved businesses can have a WhatsApp Business setup.";
            return RedirectToAction("Index", "AdminDashboard");
        }

        var settings = await db.BusinessWhatsAppSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == id, cancellationToken);

        var model = new BusinessIntegrationViewModel
        {
            BusinessId = business.Id,
            BusinessName = business.Name,
            BusinessType = business.BusinessType,
            WhatsAppNumber = business.WhatsAppNumber,
            ServicePlan = business.ServicePlan,
            WabaId = settings?.WabaId ?? string.Empty,
            DisplayName = settings?.DisplayName ?? business.Name,
            AboutText = settings?.AboutText ?? business.Description,
            BusinessCategory = settings?.BusinessCategory ?? business.BusinessType,
            WelcomeMessage = settings?.WelcomeMessage ?? $"Welcome to {business.Name}! 👋\n\nHow can we help you today?",
            PhoneNumberId = !string.IsNullOrWhiteSpace(settings?.PhoneNumberId)
                ? settings!.PhoneNumberId
                : configuration["WhatsApp:PhoneNumberId"] ?? string.Empty,
            // Never send the real access token back to the browser. A blank field means
            // "keep the business token, otherwise use the platform User Secret/config token".
            AccessToken = string.Empty,
            WebhookVerifyToken = settings?.WebhookVerifyToken ?? string.Empty,
            GraphApiVersion = !string.IsNullOrWhiteSpace(settings?.GraphApiVersion)
                ? settings!.GraphApiVersion
                : (configuration["WhatsApp:GraphApiVersion"] ?? "v26.0"),
            IsEnabled = settings?.IsEnabled ?? false,
            FeaturedProductLimit = Math.Clamp(settings?.FeaturedProductLimit ?? 6, 1, 50),
            PlatformWhatsAppConfigured =
                (!string.IsNullOrWhiteSpace(configuration["WhatsApp:PhoneNumberId"]) || !string.IsNullOrWhiteSpace(settings?.PhoneNumberId)) &&
                (!string.IsNullOrWhiteSpace(configuration["WhatsApp:AccessToken"]) || !string.IsNullOrWhiteSpace(settings?.AccessToken))
        };

        await LoadWhatsAppProductsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Manage(BusinessIntegrationViewModel model, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.FirstOrDefaultAsync(x => x.Id == model.BusinessId, cancellationToken);
        if (business is null) return NotFound();
        if (business.Status != BusinessStatuses.Approved)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Approve the business before configuring WhatsApp.";
            return RedirectToAction("Index", "AdminDashboard");
        }

        if (!ModelState.IsValid)
        {
            await LoadWhatsAppProductsAsync(model, cancellationToken, preserveSubmittedSelection: true);
            return View(model);
        }

        var platformPhoneNumberId = configuration["WhatsApp:PhoneNumberId"]?.Trim() ?? string.Empty;
        var platformAccessToken = configuration["WhatsApp:AccessToken"]?.Trim() ?? string.Empty;
        var platformWebhookToken = configuration["WhatsApp:WebhookVerifyToken"]?.Trim() ?? string.Empty;
        var platformGraphApiVersion = configuration["WhatsApp:GraphApiVersion"]?.Trim() ?? "v26.0";

        if (model.IsEnabled && string.IsNullOrWhiteSpace(model.PhoneNumberId))
            model.PhoneNumberId = platformPhoneNumberId;

        if (string.IsNullOrWhiteSpace(model.GraphApiVersion))
            model.GraphApiVersion = platformGraphApiVersion;

        var existingSettingsForValidation = await db.BusinessWhatsAppSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == model.BusinessId, cancellationToken);

        // The access token is a secret and may live only in User Secrets / environment
        // configuration. It does not need to be entered in the admin form.
        if (model.IsEnabled &&
            string.IsNullOrWhiteSpace(model.AccessToken) &&
            string.IsNullOrWhiteSpace(existingSettingsForValidation?.AccessToken) &&
            string.IsNullOrWhiteSpace(platformAccessToken))
        {
            ModelState.AddModelError(nameof(model.AccessToken), "Access Token is not configured. Add WhatsApp:AccessToken to User Secrets or environment configuration.");
            await LoadWhatsAppProductsAsync(model, cancellationToken, preserveSubmittedSelection: true);
            return View(model);
        }

        // Webhook verification is NOT required for outbound-message testing.
        // It can be configured later when the public webhook endpoint is ready.
        if (string.IsNullOrWhiteSpace(model.WebhookVerifyToken))
            model.WebhookVerifyToken = existingSettingsForValidation?.WebhookVerifyToken ?? platformWebhookToken;

        if (model.IsEnabled && string.IsNullOrWhiteSpace(model.PhoneNumberId))
        {
            ModelState.AddModelError(nameof(model.PhoneNumberId), "Phone Number ID is not configured. Add WhatsApp:PhoneNumberId to User Secrets or enter it for this business.");
            await LoadWhatsAppProductsAsync(model, cancellationToken, preserveSubmittedSelection: true);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.PhoneNumberId))
        {
            var duplicate = await db.BusinessWhatsAppSettings.AnyAsync(
                x => x.BusinessId != model.BusinessId && x.PhoneNumberId == model.PhoneNumberId.Trim() && !string.IsNullOrWhiteSpace(x.PhoneNumberId),
                cancellationToken);
            if (duplicate)
            {
                ModelState.AddModelError(nameof(model.PhoneNumberId), "This WhatsApp Phone Number ID is already mapped to another business.");
                await LoadWhatsAppProductsAsync(model, cancellationToken, preserveSubmittedSelection: true);
                return View(model);
            }
        }

        var settings = await db.BusinessWhatsAppSettings
            .FirstOrDefaultAsync(x => x.BusinessId == model.BusinessId, cancellationToken);
        var isNew = settings is null;
        settings ??= new BusinessWhatsAppSettings { BusinessId = model.BusinessId };

        settings.WabaId = model.WabaId.Trim();
        settings.DisplayName = model.DisplayName.Trim();
        settings.AboutText = model.AboutText.Trim();
        settings.BusinessCategory = model.BusinessCategory.Trim();
        settings.WelcomeMessage = model.WelcomeMessage.Trim();
        settings.PhoneNumberId = string.IsNullOrWhiteSpace(model.PhoneNumberId)
            ? platformPhoneNumberId
            : model.PhoneNumberId.Trim();
        // Keep the real Meta token out of the business database when it is supplied
        // through User Secrets/environment configuration. Only an explicitly entered
        // business token is persisted here. The WhatsApp service falls back to config.
        if (!string.IsNullOrWhiteSpace(model.AccessToken))
            settings.AccessToken = model.AccessToken.Trim();
        settings.WebhookVerifyToken = model.WebhookVerifyToken?.Trim() ?? string.Empty;
        settings.GraphApiVersion = string.IsNullOrWhiteSpace(model.GraphApiVersion) ? platformGraphApiVersion : model.GraphApiVersion.Trim();
        settings.IsEnabled = model.IsEnabled;
        settings.FeaturedProductLimit = Math.Clamp(model.FeaturedProductLimit, 1, 50);
        if (isNew) db.BusinessWhatsAppSettings.Add(settings);

        var selectedIds = (model.SelectedWhatsAppProductIds ?? new List<int>()).Distinct().ToList();
        var businessProducts = await db.BusinessCatalogItems
            .Where(x => x.BusinessId == business.Id && x.IsActive)
            .ToListAsync(cancellationToken);

        if (selectedIds.Count > settings.FeaturedProductLimit)
        {
            ModelState.AddModelError(nameof(model.SelectedWhatsAppProductIds),
                $"Select no more than {settings.FeaturedProductLimit} WhatsApp products.");
            await LoadWhatsAppProductsAsync(model, cancellationToken, preserveSubmittedSelection: true);
            return View(model);
        }

        if (selectedIds.Except(businessProducts.Select(x => x.Id)).Any())
        {
            ModelState.AddModelError(nameof(model.SelectedWhatsAppProductIds),
                "One or more selected products do not belong to this business or are inactive.");
            await LoadWhatsAppProductsAsync(model, cancellationToken, preserveSubmittedSelection: true);
            return View(model);
        }

        // Validate submitted ranks server-side. The browser UI also constrains these values,
        // but production code must not trust client-side validation.
        foreach (var selectedId in selectedIds)
        {
            if (!model.WhatsAppProductRanks.TryGetValue(selectedId, out var submittedRank) || submittedRank < 1 || submittedRank > settings.FeaturedProductLimit)
            {
                ModelState.AddModelError(nameof(model.WhatsAppProductRanks),
                    $"Every selected WhatsApp product must have a rank from 1 to {settings.FeaturedProductLimit}.");
                break;
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadWhatsAppProductsAsync(model, cancellationToken, preserveSubmittedSelection: true);
            return View(model);
        }

        var selectedSet = selectedIds.ToHashSet();
        var oldSelection = businessProducts
            .Where(x => x.IsWhatsAppTopPick)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        // Normalize the submitted ranking to 1..N. This means the WhatsApp payload always
        // follows the admin's ranking, even if two products were given the same rank.
        var ranked = selectedIds
            .Select(id => new
            {
                Id = id,
                Rank = model.WhatsAppProductRanks.TryGetValue(id, out var rank) ? rank : int.MaxValue
            })
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Id)
            .ToList();

        for (var index = 0; index < ranked.Count; index++)
        {
            var product = businessProducts.First(x => x.Id == ranked[index].Id);
            product.IsWhatsAppTopPick = true;
            product.SortOrder = index + 1;
        }

        foreach (var product in businessProducts.Where(x => !selectedSet.Contains(x.Id)))
            product.IsWhatsAppTopPick = false;

        var newSelection = businessProducts
            .Where(x => x.IsWhatsAppTopPick)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(business.Id);

        await auditLogService.WriteAsync(
            "WhatsAppBusinessSetupChanged",
            "BusinessWhatsAppSettings",
            business.Id.ToString(),
            business.Id,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                whatsappEnabled = settings.IsEnabled,
                phoneNumberId = settings.PhoneNumberId,
                featuredProductLimit = settings.FeaturedProductLimit,
                whatsappProducts = newSelection.Select(id => new
                {
                    productId = id,
                    rank = businessProducts.First(x => x.Id == id).SortOrder
                }).ToArray()
            }),
            cancellationToken);

        if (!oldSelection.SequenceEqual(newSelection))
        {
            await auditLogService.WriteAsync(
                AuditActions.WhatsAppShowcaseChanged,
                "BusinessCatalog",
                business.Id.ToString(),
                business.Id,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    previousProductIds = oldSelection,
                    selectedProductIds = newSelection
                }),
                cancellationToken);
        }

        logger.LogInformation("WhatsApp Business setup saved. BusinessId={BusinessId}", business.Id);
        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"WhatsApp Business setup for {business.Name} was saved. Selected products will be sent in rank order.";
        return RedirectToAction(nameof(Manage), new { id = business.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendCatalog(int id, string testRecipientPhoneNumber, string catalogSendMode = "selected", CancellationToken cancellationToken = default)
    {
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (business is null) return NotFound();
        if (business.Status != BusinessStatuses.Approved)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Business must be approved before sending a WhatsApp catalog.";
            return RedirectToAction(nameof(Manage), new { id });
        }

        var result = await whatsAppService.SendCatalogForBusinessAsync(
            id,
            testRecipientPhoneNumber,
            catalogSendMode,
            cancellationToken);

        TempData["ToastType"] = result.Success ? "success" : "error";
        TempData["ToastMessage"] = result.Message;
        return RedirectToAction(nameof(Manage), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestWhatsApp(int id, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (business is null) return NotFound();
        if (business.Status != BusinessStatuses.Approved) return BadRequest("Business must be approved first.");

        var result = await whatsAppService.TestConnectionForBusinessAsync(id, cancellationToken);
        TempData["ToastType"] = result.Success ? "success" : "error";
        TempData["ToastMessage"] = result.Message;
        return RedirectToAction(nameof(Manage), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTestMessage(int id, string testRecipientPhoneNumber, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (business is null) return NotFound();

        var result = await whatsAppService.SendTestMessageForBusinessAsync(id, testRecipientPhoneNumber, cancellationToken);
        TempData["ToastType"] = result.Success ? "success" : "error";
        TempData["ToastMessage"] = result.Message;
        return RedirectToAction(nameof(Manage), new { id });
    }

    private async Task LoadWhatsAppProductsAsync(
        BusinessIntegrationViewModel model,
        CancellationToken cancellationToken,
        bool preserveSubmittedSelection = false)
    {
        var products = await db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == model.BusinessId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var selectedIds = products.Where(x => x.IsWhatsAppTopPick).Select(x => x.Id).ToHashSet();
        var ranks = products.Where(x => x.IsWhatsAppTopPick)
            .ToDictionary(x => x.Id, x => Math.Max(1, x.SortOrder));

        if (preserveSubmittedSelection)
        {
            selectedIds = (model.SelectedWhatsAppProductIds ?? new List<int>()).ToHashSet();
            ranks = model.WhatsAppProductRanks ?? new Dictionary<int, int>();
        }

        model.SelectedWhatsAppProductIds = selectedIds.ToList();
        model.WhatsAppProductRanks = ranks;
        model.WhatsAppProducts = products.Select(x => new WhatsAppFeaturedProductViewModel
        {
            Id = x.Id,
            Name = x.Name,
            SKU = x.SKU,
            Category = x.Category,
            PriceText = x.PriceText,
            ImageUrl = x.ImageUrl,
            IsSelected = selectedIds.Contains(x.Id),
            IsActive = x.IsActive,
            Rank = ranks.TryGetValue(x.Id, out var rank) ? rank : 0
        }).ToList();
    }
}

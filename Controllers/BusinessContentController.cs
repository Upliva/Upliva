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
public class BusinessContentController(
    UplivaDbContext db,
    IAuditLogService auditLogService,
    IBusinessCacheService businessCache,
    ICatalogTemplateService catalogTemplateService,
    ICatalogImportService catalogImportService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id, string? category, string? search, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(id);
        if (businessId is null) return Forbid();

        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();
        if (!await CanManageCatalogAsync(business.Id, cancellationToken)) return Forbid();

        await LoadCatalogAsync(business, category, search, cancellationToken);
        return View(new BusinessCatalogItemViewModel { BusinessId = business.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCatalog(BusinessCatalogItemViewModel catalogModel, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(catalogModel.BusinessId);
        if (businessId is null) return Forbid();
        if (!await CanManageCatalogAsync(businessId.Value, cancellationToken)) return Forbid();

        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();

        await ValidateSkuAsync(businessId.Value, catalogModel.SKU, null, cancellationToken);
        var template = catalogTemplateService.GetTemplate(ResolveTemplateKey(business));
        var attributes = ValidateAndNormalizeCustomAttributes(template, catalogModel.CustomAttributes);

        if (!ModelState.IsValid)
        {
            await LoadCatalogAsync(business, null, null, cancellationToken);
            ViewBag.CatalogTemplate = template;
            catalogModel.BusinessId = businessId.Value;
            return View(nameof(Index), catalogModel);
        }

        var item = new BusinessCatalogItem
        {
            BusinessId = businessId.Value,
            Name = (catalogModel.Name ?? string.Empty).Trim(),
            Brand = (catalogModel.Brand ?? string.Empty).Trim(),
            Model = (catalogModel.Model ?? string.Empty).Trim(),
            SKU = (catalogModel.SKU ?? string.Empty).Trim(),
            Category = (catalogModel.Category ?? string.Empty).Trim(),
            PriceText = (catalogModel.PriceText ?? string.Empty).Trim(),
            OriginalPriceText = (catalogModel.OriginalPriceText ?? string.Empty).Trim(),
            DiscountText = (catalogModel.DiscountText ?? string.Empty).Trim(),
            ImageUrl = (catalogModel.ImageUrl ?? string.Empty).Trim(),
            ShortDescription = (catalogModel.ShortDescription ?? string.Empty).Trim(),
            Description = (catalogModel.Description ?? string.Empty).Trim(),
            CustomAttributesJson = JsonSerializer.Serialize(attributes),
            StockStatus = (catalogModel.StockStatus ?? string.Empty).Trim(),
            Rating = catalogModel.Rating,
            ReviewCount = catalogModel.ReviewCount,
            IsWhatsAppTopPick = false,
            IsActive = true,
            SortOrder = catalogModel.SortOrder
        };

        db.BusinessCatalogItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(businessId.Value);
        await auditLogService.WriteAsync(AuditActions.CatalogAdded, "BusinessCatalogItem", item.Id.ToString(), businessId.Value,
            JsonSerializer.Serialize(new { item.Name, item.Category, item.IsWhatsAppTopPick }), cancellationToken);

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"{item.Name} was added to the WhatsApp catalog.";
        return RedirectToAction(nameof(Index), new { id = businessId.Value });
    }

    [HttpGet]
    public async Task<IActionResult> EditCatalog(int id, CancellationToken cancellationToken)
    {
        var item = await db.BusinessCatalogItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();

        var businessId = ResolveBusinessId(item.BusinessId);
        if (businessId is null || !await CanManageCatalogAsync(businessId.Value, cancellationToken)) return Forbid();

        var business = await db.Businesses.AsNoTracking().FirstAsync(x => x.Id == businessId.Value, cancellationToken);
        await LoadCatalogAsync(business, null, null, cancellationToken);
        ViewBag.IsEditingCatalog = true;
        ViewBag.CatalogTemplate = catalogTemplateService.GetTemplate(ResolveTemplateKey(business));

        return View(nameof(Index), ToViewModel(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCatalog(int id, int businessId, CancellationToken cancellationToken)
    {
        // Do not rely on MVC's complex-object model binding for catalog updates. The edit
        // screen contains a dynamic dictionary whose keys come from CatalogTemplates/*.json.
        // Reading the submitted form explicitly makes the update path deterministic for both
        // common fields and dynamic fields.
        ModelState.Clear();
        var form = await Request.ReadFormAsync(cancellationToken);

        var existing = await db.BusinessCatalogItems
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing is null) return NotFound();

        // Never trust the posted business id. The catalog row is the source of truth.
        if (businessId != existing.BusinessId) return BadRequest("The catalog business does not match the selected item.");

        var resolvedBusinessId = ResolveBusinessId(existing.BusinessId);
        if (resolvedBusinessId is null || !await CanManageCatalogAsync(resolvedBusinessId.Value, cancellationToken)) return Forbid();

        var business = await db.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == resolvedBusinessId.Value, cancellationToken);
        if (business is null) return NotFound();

        var template = catalogTemplateService.GetTemplate(ResolveTemplateKey(business));
        var model = BuildCatalogModelFromForm(form, existing, business.Id, template);
        ValidateCatalogModel(model, business.Id, existing.Id, template, cancellationToken);
        await ValidateSkuAsync(business.Id, model.SKU, existing.Id, cancellationToken);

        if (!ModelState.IsValid)
        {
            await LoadCatalogAsync(business, null, null, cancellationToken);
            ViewBag.IsEditingCatalog = true;
            ViewBag.CatalogTemplate = template;
            return View(nameof(Index), model);
        }

        existing.Name = (model.Name ?? string.Empty).Trim();
        existing.Brand = (model.Brand ?? string.Empty).Trim();
        existing.Model = (model.Model ?? string.Empty).Trim();
        existing.SKU = (model.SKU ?? string.Empty).Trim();
        existing.Category = (model.Category ?? string.Empty).Trim();
        existing.PriceText = (model.PriceText ?? string.Empty).Trim();
        existing.OriginalPriceText = (model.OriginalPriceText ?? string.Empty).Trim();
        existing.DiscountText = (model.DiscountText ?? string.Empty).Trim();
        existing.ImageUrl = (model.ImageUrl ?? string.Empty).Trim();
        existing.ShortDescription = (model.ShortDescription ?? string.Empty).Trim();
        existing.Description = (model.Description ?? string.Empty).Trim();
        existing.CustomAttributesJson = JsonSerializer.Serialize(model.CustomAttributes);
        existing.StockStatus = (model.StockStatus ?? string.Empty).Trim();
        existing.Rating = model.Rating;
        existing.ReviewCount = model.ReviewCount;
        existing.SortOrder = model.SortOrder;

        await db.SaveChangesAsync(cancellationToken);

        // Verify the actual persisted values before redirecting. This catches accidental
        // connection-string/database mismatches during testing instead of showing a false success.
        var saved = await db.BusinessCatalogItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == existing.Id, cancellationToken);
        if (saved is null || !string.Equals(saved.Name, existing.Name, StringComparison.Ordinal) ||
            !string.Equals(saved.PriceText, existing.PriceText, StringComparison.Ordinal) ||
            !string.Equals(saved.CustomAttributesJson, existing.CustomAttributesJson, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "The catalog update could not be verified in SQL Server. Please check the active database connection and try again.");
            await LoadCatalogAsync(business, null, null, cancellationToken);
            ViewBag.IsEditingCatalog = true;
            ViewBag.CatalogTemplate = template;
            return View(nameof(Index), model);
        }

        businessCache.InvalidateBusiness(business.Id);
        await auditLogService.WriteAsync(
            AuditActions.CatalogUpdated,
            "BusinessCatalogItem",
            existing.Id.ToString(),
            business.Id,
            JsonSerializer.Serialize(new { existing.Name, existing.Category, existing.IsWhatsAppTopPick, existing.SortOrder }),
            cancellationToken);

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"{existing.Name} was updated successfully.";
        return RedirectToAction(nameof(Index), new { id = business.Id });
    }

    private BusinessCatalogItemViewModel BuildCatalogModelFromForm(
        IFormCollection form,
        BusinessCatalogItem existing,
        int businessId,
        CatalogTemplateDefinition template)
    {
        string Get(string key) => form.TryGetValue(key, out var value) ? value.ToString() : string.Empty;

        var model = new BusinessCatalogItemViewModel
        {
            Id = existing.Id,
            BusinessId = businessId,
            Name = Get("Name"),
            Brand = Get("Brand"),
            Model = Get("Model"),
            SKU = Get("SKU"),
            Category = Get("Category"),
            PriceText = Get("PriceText"),
            OriginalPriceText = Get("OriginalPriceText"),
            DiscountText = Get("DiscountText"),
            ImageUrl = Get("ImageUrl"),
            ShortDescription = Get("ShortDescription"),
            Description = Get("Description"),
            StockStatus = Get("StockStatus"),
            Rating = TryParseDecimal(Get("Rating")),
            ReviewCount = TryParseInt(Get("ReviewCount")),
            SortOrder = TryParseInt(Get("SortOrder")),
            IsWhatsAppTopPick = existing.IsWhatsAppTopPick,
            CustomAttributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        // Start with the existing JSON so fields introduced by a newer template are not
        // accidentally destroyed. For every field currently rendered on the form, the
        // posted value becomes the source of truth; clearing a field removes its value.
        foreach (var existingAttribute in DeserializeAttributes(existing.CustomAttributesJson))
            model.CustomAttributes[existingAttribute.Key] = existingAttribute.Value;

        foreach (var field in template.Fields.OrderBy(x => x.SortOrder))
        {
            var key = $"CustomAttributes[{field.Key}]";
            var value = Get(key).Trim();
            if (value.Length > 0)
                model.CustomAttributes[field.Key] = value;
            else
                model.CustomAttributes.Remove(field.Key);
        }

        return model;
    }

    private void ValidateCatalogModel(
        BusinessCatalogItemViewModel model,
        int businessId,
        int currentId,
        CatalogTemplateDefinition template,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(BusinessCatalogItemViewModel.Name), "Product name is required.");
        else if ((model.Name ?? string.Empty).Trim().Length > 150)
            ModelState.AddModelError(nameof(BusinessCatalogItemViewModel.Name), "Product name cannot exceed 150 characters.");

        ValidateLength(model.Brand ?? string.Empty, 100, nameof(model.Brand));
        ValidateLength(model.Model ?? string.Empty, 100, nameof(model.Model));
        ValidateLength(model.SKU ?? string.Empty, 80, nameof(model.SKU));
        ValidateLength(model.Category ?? string.Empty, 80, nameof(model.Category));
        ValidateLength(model.PriceText ?? string.Empty, 100, nameof(model.PriceText));
        ValidateLength(model.OriginalPriceText ?? string.Empty, 100, nameof(model.OriginalPriceText));
        ValidateLength(model.DiscountText ?? string.Empty, 80, nameof(model.DiscountText));
        ValidateLength(model.ImageUrl ?? string.Empty, 500, nameof(model.ImageUrl));
        ValidateLength(model.ShortDescription ?? string.Empty, 300, nameof(model.ShortDescription));
        ValidateLength(model.Description ?? string.Empty, 1000, nameof(model.Description));
        ValidateLength(model.StockStatus ?? string.Empty, 80, nameof(model.StockStatus));

        if (!string.IsNullOrWhiteSpace(model.ImageUrl) && !Uri.TryCreate((model.ImageUrl ?? string.Empty).Trim(), UriKind.Absolute, out _))
            ModelState.AddModelError(nameof(model.ImageUrl), "Product image URL must be a valid absolute URL.");
        if (model.Rating is < 0 or > 5)
            ModelState.AddModelError(nameof(model.Rating), "Rating must be between 0 and 5.");
        if (model.ReviewCount < 0)
            ModelState.AddModelError(nameof(model.ReviewCount), "Review count cannot be negative.");
        if (model.SortOrder < 0)
            ModelState.AddModelError(nameof(model.SortOrder), "Display order cannot be negative.");

        foreach (var field in template.Fields.OrderBy(x => x.SortOrder))
        {
            model.CustomAttributes.TryGetValue(field.Key, out var value);
            value ??= string.Empty;
            if (field.Type.Equals("select", StringComparison.OrdinalIgnoreCase) && value.Length > 0 && field.Options.Count > 0 && !field.Options.Contains(value, StringComparer.OrdinalIgnoreCase))
                ModelState.AddModelError($"CustomAttributes[{field.Key}]", $"Select a valid value for {field.Label}.");
            if (field.Type.Equals("url", StringComparison.OrdinalIgnoreCase) && value.Length > 0 && !Uri.TryCreate(value, UriKind.Absolute, out _))
                ModelState.AddModelError($"CustomAttributes[{field.Key}]", $"Enter a valid URL for {field.Label}.");
        }
    }

    private void ValidateLength(string value, int maxLength, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
            ModelState.AddModelError(fieldName, $"{fieldName} cannot exceed {maxLength} characters.");
    }

    private static decimal? TryParseDecimal(string value) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;

    private static int TryParseInt(string value) =>
        int.TryParse(value, out var result) && result >= 0 ? result : 0;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCatalog(int id, CancellationToken cancellationToken)
    {
        var item = await db.BusinessCatalogItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();

        var businessId = ResolveBusinessId(item.BusinessId);
        if (businessId is null || !await CanManageCatalogAsync(businessId.Value, cancellationToken)) return Forbid();

        db.BusinessCatalogItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(businessId.Value);
        await auditLogService.WriteAsync(AuditActions.CatalogDeleted, "BusinessCatalogItem", item.Id.ToString(), businessId.Value,
            JsonSerializer.Serialize(new { item.Name }), cancellationToken);

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"{item.Name} was removed from the catalog.";
        return RedirectToAction(nameof(Index), new { id = businessId.Value });
    }

    [HttpGet]
    public async Task<IActionResult> ImportCatalog(int id, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(id);
        if (businessId is null || !await CanManageCatalogAsync(businessId.Value, cancellationToken)) return Forbid();
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();
        var template = catalogTemplateService.GetTemplate(ResolveTemplateKey(business));
        ViewBag.CatalogFields = template.Fields.OrderBy(x => x.SortOrder).ToList();
        return View(new CatalogImportViewModel
        {
            BusinessId = business.Id, BusinessName = business.Name,
            TemplateKey = template.Key, TemplateDisplayName = template.DisplayName
        });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCatalogTemplate(int id, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(id);
        if (businessId is null || !await CanManageCatalogAsync(businessId.Value, cancellationToken)) return Forbid();
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();
        var template = catalogTemplateService.GetTemplate(ResolveTemplateKey(business));
        var csv = CatalogImportService.CreateCsvTemplate(template);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{template.Key}-catalog-template.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportCatalog(CatalogImportViewModel model, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(model.BusinessId);
        if (businessId is null || !await CanManageCatalogAsync(businessId.Value, cancellationToken)) return Forbid();
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();
        var template = catalogTemplateService.GetTemplate(ResolveTemplateKey(business));
        ViewBag.CatalogFields = template.Fields.OrderBy(x => x.SortOrder).ToList();
        model.BusinessName = business.Name; model.TemplateKey = template.Key; model.TemplateDisplayName = template.DisplayName;

        if (model.File is null || model.File.Length == 0)
        {
            model.Errors.Add("Please choose a CSV or JSON file.");
            return View(model);
        }

        var parsed = await catalogImportService.ParseAsync(model.File, template, cancellationToken);
        model.Format = parsed.Format;
        model.Errors.AddRange(parsed.Errors);
        if (parsed.Records.Count == 0)
        {
            if (model.Errors.Count == 0) model.Errors.Add("No catalog rows were found.");
            return View(model);
        }

        if (parsed.Records.Count > 500)
        {
            model.Errors.Add("A single import is limited to 500 catalog rows. Split the file into smaller files.");
            return View(model);
        }

        var records = new List<BusinessCatalogItem>();
        var importSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in parsed.Records)
        {
            var item = BuildImportedCatalogItem(business.Id, record, template, out var rowErrors);
            if (!string.IsNullOrWhiteSpace(item.SKU))
            {
                if (!importSkus.Add(item.SKU)) rowErrors.Add($"SKU '{item.SKU}' is duplicated in the import file.");
                if (await db.BusinessCatalogItems.AnyAsync(x => x.BusinessId == business.Id && x.IsActive && x.SKU == item.SKU, cancellationToken))
                    rowErrors.Add($"SKU '{item.SKU}' already exists in this business catalog.");
            }
            if (rowErrors.Count > 0)
            {
                foreach (var error in rowErrors) model.Errors.Add($"Row {record.RowNumber}: {error}");
                continue;
            }
            records.Add(item);
            model.PreviewRows.Add(new CatalogImportPreviewRow
            {
                RowNumber = record.RowNumber, ProductName = item.Name, Category = item.Category, PriceText = item.PriceText, Status = "Ready"
            });
        }
        model.PreviewRowCount = model.PreviewRows.Count;

        // All-or-nothing import: no partial catalog is inserted when one row is invalid.
        if (model.Errors.Count > 0) return View(model);

        db.BusinessCatalogItems.AddRange(records);
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(business.Id);
        foreach (var item in records)
            await auditLogService.WriteAsync(AuditActions.CatalogAdded, "BusinessCatalogItem", item.Id.ToString(), business.Id,
                JsonSerializer.Serialize(new { item.Name, import = true }), cancellationToken);

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"Imported {records.Count} catalog item(s) from {model.Format.ToUpperInvariant()}.";
        return RedirectToAction(nameof(Index), new { id = business.Id });
    }

    private BusinessCatalogItem BuildImportedCatalogItem(int businessId, CatalogImportRecord record, CatalogTemplateDefinition template, out List<string> errors)
    {
        errors = [];
        string Get(string key) => record.Values.TryGetValue(key, out var value) ? value.Trim() : string.Empty;
        var name = Get("Name");
        if (string.IsNullOrWhiteSpace(name)) errors.Add("Product Name is required.");
        ValidateLength(Get("Brand"), 100, "Brand", errors);
        ValidateLength(Get("Model"), 100, "Model", errors);
        ValidateLength(Get("SKU"), 80, "SKU", errors);
        ValidateLength(Get("Category"), 80, "Category", errors);
        ValidateLength(Get("PriceText"), 100, "Price", errors);
        ValidateLength(Get("OriginalPriceText"), 100, "Original / MRP price", errors);
        ValidateLength(Get("DiscountText"), 80, "Discount", errors);
        ValidateLength(Get("ImageUrl"), 500, "Image URL", errors);
        ValidateLength(Get("ShortDescription"), 300, "Short description", errors);
        ValidateLength(Get("Description"), 1000, "Description", errors);
        ValidateLength(Get("StockStatus"), 80, "Stock status", errors);
        if (name.Length > 150) errors.Add("Product Name cannot exceed 150 characters.");
        var imageUrl = Get("ImageUrl");
        if (imageUrl.Length > 0 && !Uri.TryCreate(imageUrl, UriKind.Absolute, out _)) errors.Add("Image URL must be a valid absolute URL.");
        var ratingText = Get("Rating");
        decimal? rating = null;
        if (!string.IsNullOrWhiteSpace(ratingText))
        {
            if (decimal.TryParse(ratingText, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsedRating) && parsedRating is >= 0 and <= 5) rating = parsedRating;
            else errors.Add("Rating must be a number from 0 to 5.");
        }
        var reviewText = Get("ReviewCount");
        var reviews = 0;
        if (!string.IsNullOrWhiteSpace(reviewText) && (!int.TryParse(reviewText, out reviews) || reviews < 0)) errors.Add("Review Count must be a non-negative whole number.");
        var sortText = Get("SortOrder");
        var sort = 0;
        if (!string.IsNullOrWhiteSpace(sortText) && (!int.TryParse(sortText, out sort) || sort < 0)) errors.Add("Sort Order must be a non-negative whole number.");

        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in template.Fields)
        {
            var value = Get(field.Key);
            if (field.Type.Equals("select", StringComparison.OrdinalIgnoreCase) && value.Length > 0 && field.Options.Count > 0 && !field.Options.Contains(value, StringComparer.OrdinalIgnoreCase))
                errors.Add($"{field.Label} must be one of: {string.Join(", ", field.Options)}.");
            if (field.Type.Equals("url", StringComparison.OrdinalIgnoreCase) && value.Length > 0 && !Uri.TryCreate(value, UriKind.Absolute, out _))
                errors.Add($"{field.Label} must be a valid URL.");
            if (value.Length > 0) attributes[field.Key] = value;
        }

        return new BusinessCatalogItem
        {
            BusinessId = businessId, Name = name, Brand = Get("Brand"), Model = Get("Model"), SKU = Get("SKU"), Category = Get("Category"),
            PriceText = Get("PriceText"), OriginalPriceText = Get("OriginalPriceText"), DiscountText = Get("DiscountText"), ImageUrl = Get("ImageUrl"),
            ShortDescription = Get("ShortDescription"), Description = Get("Description"), StockStatus = Get("StockStatus"), Rating = rating,
            ReviewCount = reviews, SortOrder = sort, IsActive = true, IsWhatsAppTopPick = false, CustomAttributesJson = JsonSerializer.Serialize(attributes)
        };
    }

    [HttpGet]
    public async Task<IActionResult> PreviewWhatsApp(int id, int? productId, CancellationToken cancellationToken)
    {
        var businessId = ResolveBusinessId(id);
        if (businessId is null) return Forbid();

        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId.Value, cancellationToken);
        if (business is null) return NotFound();
        if (!await CanManageCatalogAsync(business.Id, cancellationToken)) return Forbid();

        var template = catalogTemplateService.GetTemplate(ResolveTemplateKey(business));
        var catalog = await db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive && x.IsWhatsAppTopPick)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var focus = productId.HasValue
            ? await db.BusinessCatalogItems.AsNoTracking().FirstOrDefaultAsync(x => x.BusinessId == business.Id && x.Id == productId.Value && x.IsActive, cancellationToken)
            : null;

        if (productId.HasValue && focus is null) return NotFound();

        return View(new WhatsAppPreviewViewModel
        {
            BusinessId = business.Id,
            BusinessName = business.Name,
            BusinessType = business.BusinessType,
            WhatsAppNumber = business.WhatsAppNumber,
            ServicePlan = business.ServicePlan,
            Tagline = business.Tagline,
            LogoUrl = business.LogoUrl,
            Products = catalog.Select(x => MapPreview(x, template)).ToList(),
            FocusProduct = focus is null ? null : MapPreview(focus, template)
        });
    }

    private WhatsAppPreviewProductViewModel MapPreview(BusinessCatalogItem item, CatalogTemplateDefinition template) => new()
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
        Rank = item.IsWhatsAppTopPick ? item.SortOrder : 0,
        CustomFields = catalogTemplateService.GetDisplayFields(template.Key, item.CustomAttributesJson, true).ToList()
    };

    private async Task LoadCatalogAsync(Business business, string? category, string? search, CancellationToken cancellationToken)
    {
        var all = await db.BusinessCatalogItems.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive)
            .OrderBy(x => x.Category).ThenBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var q = search?.Trim() ?? string.Empty;
        var cat = category?.Trim() ?? string.Empty;

        ViewBag.Business = business;
        ViewBag.Catalog = all.Where(x =>
            (string.IsNullOrWhiteSpace(cat) || string.Equals(x.Category, cat, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(q) || x.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || x.Category.Contains(q, StringComparison.OrdinalIgnoreCase) || x.SKU.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
        ViewBag.AllCatalogCount = all.Count;
        ViewBag.TopPickCount = all.Count(x => x.IsWhatsAppTopPick);
        ViewBag.Categories = all.Select(x => x.Category.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        ViewBag.SelectedCategory = cat;
        ViewBag.Search = q;
        ViewBag.CatalogTemplate = catalogTemplateService.GetTemplate(ResolveTemplateKey(business));
    }

    private async Task ValidateSkuAsync(int businessId, string? sku, int? currentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sku)) return;
        var normalized = sku.Trim();
        if (await db.BusinessCatalogItems.AnyAsync(x => x.BusinessId == businessId && x.Id != currentId && x.IsActive && x.SKU == normalized, cancellationToken))
            ModelState.AddModelError(nameof(BusinessCatalogItemViewModel.SKU), "This SKU is already used by another active catalog item.");
    }

    private Dictionary<string, string> ValidateAndNormalizeCustomAttributes(CatalogTemplateDefinition template, Dictionary<string, string>? postedValues)
    {
        var source = postedValues ?? new Dictionary<string, string>();
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in template.Fields.OrderBy(x => x.SortOrder))
        {
            source.TryGetValue(field.Key, out var raw);
            var value = raw?.Trim() ?? string.Empty;

            if (field.Type.Equals("select", StringComparison.OrdinalIgnoreCase) && value.Length > 0 && field.Options.Count > 0 && !field.Options.Contains(value, StringComparer.OrdinalIgnoreCase))
                ModelState.AddModelError($"CustomAttributes[{field.Key}]", $"Select a valid value for {field.Label}.");

            if (field.Type.Equals("url", StringComparison.OrdinalIgnoreCase) && value.Length > 0 && !Uri.TryCreate(value, UriKind.Absolute, out _))
                ModelState.AddModelError($"CustomAttributes[{field.Key}]", $"Enter a valid URL for {field.Label}.");

            if (value.Length > 0) normalized[field.Key] = value;
        }
        return normalized;
    }

    private BusinessCatalogItemViewModel ToViewModel(BusinessCatalogItem item) => new()
    {
        Id = item.Id, BusinessId = item.BusinessId, Name = item.Name, Brand = item.Brand, Model = item.Model, SKU = item.SKU,
        Category = item.Category, PriceText = item.PriceText, OriginalPriceText = item.OriginalPriceText, DiscountText = item.DiscountText,
        ImageUrl = item.ImageUrl, ShortDescription = item.ShortDescription, Description = item.Description,
        CustomAttributes = DeserializeAttributes(item.CustomAttributesJson), StockStatus = item.StockStatus, Rating = item.Rating,
        ReviewCount = item.ReviewCount, IsWhatsAppTopPick = item.IsWhatsAppTopPick, SortOrder = item.SortOrder
    };

    private static Dictionary<string, string> DeserializeAttributes(string? json)
    {
        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json ?? "{}") ?? new();
            return new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
        }
        catch { return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
    }

    private static void ValidateLength(string value, int maxLength, string label, List<string> errors)
    {
        if (value.Length > maxLength) errors.Add($"{label} cannot exceed {maxLength} characters.");
    }

    private async Task<bool> CanManageCatalogAsync(int businessId, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(PlatformRoles.Admin))
        {
            var claim = User.FindFirstValue("BusinessId");
            return int.TryParse(claim, out var ownerBusinessId) && ownerBusinessId == businessId;
        }

        return await db.Businesses.AsNoTracking().AnyAsync(x => x.Id == businessId && x.Status == BusinessStatuses.Approved, cancellationToken);
    }

    private string ResolveTemplateKey(Business business) =>
        string.IsNullOrWhiteSpace(business.CatalogTemplateKey) || business.CatalogTemplateKey.Equals("generic", StringComparison.OrdinalIgnoreCase)
            ? catalogTemplateService.GetTemplate(business.BusinessType).Key
            : business.CatalogTemplateKey;

    private int? ResolveBusinessId(int? requestedId)
    {
        if (User.IsInRole(PlatformRoles.Admin) && requestedId.HasValue) return requestedId;
        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

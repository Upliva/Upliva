using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Authorize(Roles = PlatformRoles.Admin)]
public class AdminWhatsAppTemplatesController(
    UplivaDbContext db,
    IGupshupWhatsAppService gupshup) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await db.WhatsAppTemplateConfigurations.AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken));

    [HttpGet]
    public IActionResult Create() => View("Edit", new WhatsAppTemplateConfiguration());

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await db.WhatsAppTemplateConfigurations.FindAsync([id], cancellationToken);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(WhatsAppTemplateConfiguration model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        model.Name = model.Name.Trim();
        model.ProviderTemplateName = model.ProviderTemplateName.Trim();
        model.Provider = model.Provider.Trim();
        model.LanguageCode = model.LanguageCode.Trim();
        model.Category = model.Category.Trim();
        model.BodyText = model.BodyText.Trim();
        model.ButtonText = model.ButtonText.Trim();
        model.ButtonUrlTemplate = model.ButtonUrlTemplate.Trim();

        if (model.Id == 0)
        {
            if (model.IsDefault)
                await db.WhatsAppTemplateConfigurations
                    .Where(x => x.IsDefault)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false), cancellationToken);

            model.CreatedAtUtc = DateTime.UtcNow;
            model.UpdatedAtUtc = DateTime.UtcNow;
            db.WhatsAppTemplateConfigurations.Add(model);
        }
        else
        {
            var existing = await db.WhatsAppTemplateConfigurations
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
            if (existing is null) return NotFound();

            if (model.IsDefault)
                await db.WhatsAppTemplateConfigurations
                    .Where(x => x.Id != model.Id && x.IsDefault)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false), cancellationToken);

            existing.Name = model.Name;
            existing.ProviderTemplateName = model.ProviderTemplateName;
            existing.Provider = model.Provider;
            existing.LanguageCode = model.LanguageCode;
            existing.Category = model.Category;
            existing.BodyText = model.BodyText;
            existing.ButtonText = model.ButtonText;
            existing.ButtonUrlTemplate = model.ButtonUrlTemplate;
            existing.IsActive = model.IsActive;
            existing.IsDefault = model.IsDefault;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "WhatsApp template saved. You can now send it from UplivaAI.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Send(int id, int? businessId, string? destination, CancellationToken cancellationToken)
    {
        var template = await db.WhatsAppTemplateConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (template is null) return NotFound();

        var businesses = await db.Businesses.AsNoTracking()
            .Where(x => x.Status == BusinessStatuses.Approved)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        ViewBag.Businesses = businesses;
        ViewBag.Placeholders = ExtractPlaceholders(template.BodyText);
        ViewBag.Template = template;

        return View(new WhatsAppTemplateSendViewModel
        {
            TemplateId = template.Id,
            TemplateName = template.Name,
            TemplateBody = template.BodyText,
            ButtonText = template.ButtonText,
            BusinessId = businessId ?? businesses.FirstOrDefault()?.Id ?? 0,
            Destination = destination ?? string.Empty,
            Parameters = ExtractPlaceholders(template.BodyText)
                .Select(x => new WhatsAppTemplateParameterViewModel { Name = x })
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(WhatsAppTemplateSendViewModel model, CancellationToken cancellationToken)
    {
        var template = await db.WhatsAppTemplateConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.TemplateId && x.IsActive, cancellationToken);
        if (template is null) return NotFound();

        var placeholderNames = ExtractPlaceholders(template.BodyText);
        var postedValues = model.Parameters.Select(x => x.Value?.Trim() ?? string.Empty).ToList();

        if (postedValues.Count != placeholderNames.Count)
            ModelState.AddModelError(string.Empty, $"Template '{template.Name}' expects {placeholderNames.Count} parameter(s).");

        if (!ModelState.IsValid)
        {
            await PrepareSendViewAsync(model, template, cancellationToken);
            return View(model);
        }

        var result = await gupshup.SendTemplateAsync(
            model.BusinessId,
            template,
            model.Destination,
            postedValues,
            cancellationToken);

        model.TemplateName = template.Name;
        model.TemplateBody = template.BodyText;
        model.ButtonText = template.ButtonText;
        model.Result = result.Message;
        model.Success = result.Success;
        model.Parameters = placeholderNames.Select((name, index) => new WhatsAppTemplateParameterViewModel
        {
            Name = name,
            Value = index < postedValues.Count ? postedValues[index] : string.Empty
        }).ToList();

        await PrepareSendViewAsync(model, template, cancellationToken, preserveBusinessSelection: true);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await db.WhatsAppTemplateConfigurations.FindAsync([id], cancellationToken);
        if (item is not null)
        {
            db.WhatsAppTemplateConfigurations.Remove(item);
            await db.SaveChangesAsync(cancellationToken);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PrepareSendViewAsync(
        WhatsAppTemplateSendViewModel model,
        WhatsAppTemplateConfiguration template,
        CancellationToken cancellationToken,
        bool preserveBusinessSelection = false)
    {
        var businesses = await db.Businesses.AsNoTracking()
            .Where(x => x.Status == BusinessStatuses.Approved)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        ViewBag.Businesses = businesses;
        ViewBag.Placeholders = ExtractPlaceholders(template.BodyText);
        ViewBag.Template = template;

        var existingValues = model.Parameters.ToDictionary(x => x.Name, x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        model.Parameters = ExtractPlaceholders(template.BodyText)
            .Select(x => new WhatsAppTemplateParameterViewModel
            {
                Name = x,
                Value = existingValues.TryGetValue(x, out var value) ? value : string.Empty
            })
            .ToList();

        if (!preserveBusinessSelection && model.BusinessId == 0)
            model.BusinessId = businesses.FirstOrDefault()?.Id ?? 0;
    }

    private static IReadOnlyList<string> ExtractPlaceholders(string body)
        => Regex.Matches(body ?? string.Empty, @"\{\{([^{}]+)\}\}")
            .Select(x => x.Groups[1].Value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

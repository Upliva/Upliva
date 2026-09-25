using Microsoft.AspNetCore.Mvc;
using UplivaAI.Services;
using UplivaAI.Models;

namespace UplivaAI.Controllers;

public class BusinessRegistrationController(
    IMarketingEngagementService marketingEngagement) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Register(CancellationToken cancellationToken)
    {
        var visitorId = Request.Cookies.TryGetValue("UplivaAI.VisitorId", out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing
            : Guid.NewGuid().ToString("N");

        if (!Request.Cookies.ContainsKey("UplivaAI.VisitorId"))
        {
            Response.Cookies.Append("UplivaAI.VisitorId", visitorId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(365)
            });
        }

        await marketingEngagement.RecordVisitAsync(visitorId, Request.Path, cancellationToken);
        return View(new LeadRegistrationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(LeadRegistrationViewModel model, CancellationToken cancellationToken)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.BusinessType = model.BusinessType?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Business name is required.");
        if (string.IsNullOrWhiteSpace(model.BusinessType))
            ModelState.AddModelError(nameof(model.BusinessType), "Business type is required.");
        var enteredPhone = new string((model.WhatsAppNumber ?? string.Empty).Where(char.IsDigit).ToArray());

        if (!BusinessInputRules.IsOptionalWhatsAppValid(enteredPhone) || string.IsNullOrWhiteSpace(enteredPhone))
        {
            ModelState.AddModelError(nameof(model.WhatsAppNumber),
                "WhatsApp number is required and must be a valid 10-digit Indian number.");
        }
        else
        {
            model.WhatsAppNumber = string.IsNullOrWhiteSpace(enteredPhone)
                ? string.Empty
                : BusinessInputRules.NormalizeWhatsApp(enteredPhone);
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var visitorId = Request.Cookies.TryGetValue("UplivaAI.VisitorId", out var value)
                ? value ?? string.Empty
                : string.Empty;

            await marketingEngagement.CaptureLeadAsync(
                model.Name ?? string.Empty,
                model.BusinessType ?? string.Empty,
                model.WhatsAppNumber ?? string.Empty,
                visitorId,
                "BusinessRegistration",
                cancellationToken);

            return RedirectToAction(nameof(Success));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Success() => View();
}

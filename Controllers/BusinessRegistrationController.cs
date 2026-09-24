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
        var enteredPhone = new string((model.WhatsAppNumber ?? string.Empty).Where(char.IsDigit).ToArray());

        if (!BusinessRegistrationInputHelper.IsValidIndianLeadWhatsAppNumber(enteredPhone))
        {
            ModelState.AddModelError(nameof(model.WhatsAppNumber),
                "Please enter your 10-digit Indian WhatsApp / mobile number.");
        }
        else
        {
            // Visitors enter only 10 digits. Store 91xxxxxxxxxx internally for WhatsApp.
            model.WhatsAppNumber = BusinessRegistrationInputHelper.NormalizeIndianLeadWhatsAppNumber(enteredPhone);
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var visitorId = Request.Cookies.TryGetValue("UplivaAI.VisitorId", out var value)
                ? value ?? string.Empty
                : string.Empty;

            await marketingEngagement.CaptureLeadAsync(
                model.Name,
                model.BusinessType,
                model.WhatsAppNumber,
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

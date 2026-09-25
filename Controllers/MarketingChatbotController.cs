using Microsoft.AspNetCore.Mvc;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Route("marketing/chatbot")]
public class MarketingChatbotController(IMarketingEngagementService engagement) : Controller
{
    [HttpPost("lead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lead(MarketingChatbotLeadRequest model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(e => !string.IsNullOrWhiteSpace(e))
                ?? "Please check the entered details.";
            return BadRequest(new { success = false, message });
        }

        var enteredPhone = new string((model.WhatsAppNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        if (!BusinessInputRules.IsOptionalWhatsAppValid(enteredPhone) || string.IsNullOrWhiteSpace(enteredPhone))
            return BadRequest(new { success = false, message = "WhatsApp number is required and must be a valid 10-digit Indian number." });

        model.WhatsAppNumber = string.IsNullOrWhiteSpace(enteredPhone)
            ? string.Empty
            : BusinessInputRules.NormalizeWhatsApp(enteredPhone);

        try
        {
            var visitorId = Request.Cookies.TryGetValue("UplivaAI.VisitorId", out var value) ? value ?? string.Empty : string.Empty;
            await engagement.CaptureLeadAsync(model.Name ?? string.Empty, model.BusinessType ?? string.Empty, model.WhatsAppNumber ?? string.Empty, visitorId, cancellationToken);
            return Ok(new { success = true, message = "Your information was received successfully. Our business team will reach out to you within 24 hours. For support and assistance, connect with us at uplivasupport@gmail.com." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class MarketingChatbotLeadRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(180)]
    public string? Name { get; set; }

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(80)]
    public string? BusinessType { get; set; }

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(20)]
    public string? WhatsAppNumber { get; set; }
}

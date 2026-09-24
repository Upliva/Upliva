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
                ?? "Please complete the three questions.";
            return BadRequest(new { success = false, message });
        }

        var enteredPhone = new string((model.WhatsAppNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        if (!BusinessRegistrationInputHelper.IsValidIndianLeadWhatsAppNumber(enteredPhone))
            return BadRequest(new { success = false, message = "Please enter your 10-digit Indian WhatsApp / mobile number." });

        model.WhatsAppNumber = BusinessRegistrationInputHelper.NormalizeIndianLeadWhatsAppNumber(enteredPhone);

        try
        {
            var visitorId = Request.Cookies.TryGetValue("UplivaAI.VisitorId", out var value) ? value ?? string.Empty : string.Empty;
            await engagement.CaptureLeadAsync(model.Name, model.BusinessType, model.WhatsAppNumber, visitorId, cancellationToken);
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
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(80)]
    public string BusinessType { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(10)]
    public string WhatsAppNumber { get; set; } = string.Empty;
}

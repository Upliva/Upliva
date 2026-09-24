using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Authorize(Roles = PlatformRoles.Admin)]
public class WhatsAppController(
    IWhatsAppService whatsapp,
    ILogger<WhatsAppController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new WhatsAppTestViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMenu(
        WhatsAppTestViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("Index", model);

        logger.LogInformation("Sending generic WhatsApp menu to {PhoneNumber}", model.PhoneNumber);

        var success = await whatsapp.SendWelcomeMenuAsync(
            model.PhoneNumber,
            businessName: "UplivaAI Demo Business",
            cancellationToken: cancellationToken);

        model.Result = success
            ? "WhatsApp menu sent successfully."
            : "WhatsApp API call failed. Check the Visual Studio Output window.";

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendImage(
        WhatsAppTestViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("Index", model);

        var imageUrl = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=1200&q=80";
        var caption =
            "UplivaAI Demo Business\n\n" +
            "A sample product image sent through the WhatsApp Business Platform.\n\n" +
            "Reply *Hi* to explore the business menu.";

        logger.LogInformation("Sending business promotional image to {PhoneNumber}", model.PhoneNumber);

        var success = await whatsapp.SendImageAsync(
            model.PhoneNumber,
            imageUrl,
            caption,
            cancellationToken);

        model.Result = success
            ? "Business image sent successfully."
            : "WhatsApp API call failed. Check the Visual Studio Output window.";

        return View("Index", model);
    }
}

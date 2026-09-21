using Microsoft.AspNetCore.Mvc;
using UplivaResortBooking.Models;
using UplivaResortBooking.Services;

namespace UplivaResortBooking.Controllers;

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

        logger.LogInformation("Sending test WhatsApp menu to {PhoneNumber}", model.PhoneNumber);

        var success = await whatsapp.SendWelcomeMenuAsync(
            model.PhoneNumber,
            cancellationToken: cancellationToken);

        model.Result = success
            ? "Welcome menu sent successfully."
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

        var imageUrl = "https://images.unsplash.com/photo-1540541338287-41700207dee6?auto=format&fit=crop&w=1200&q=80";
        var caption =
            "🏝️ Paradise Palm Resort\n\n" +
            "Relax, reconnect and enjoy a memorable stay.\n\n" +
            "🏨 Rooms from ₹4,500/night\n" +
            "🍽️ Restaurant\n" +
            "🏊 Swimming pool\n" +
            "🌴 Resort experiences\n\n" +
            "Reply *Hi* to explore booking options.";

        logger.LogInformation("Sending resort promotional image to {PhoneNumber}", model.PhoneNumber);

        var success = await whatsapp.SendImageAsync(
            model.PhoneNumber,
            imageUrl,
            caption,
            cancellationToken);

        model.Result = success
            ? "Resort image sent successfully."
            : "WhatsApp API call failed. Check the Visual Studio Output window.";

        return View("Index", model);
    }
}

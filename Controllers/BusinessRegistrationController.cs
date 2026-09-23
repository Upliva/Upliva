using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

public class BusinessRegistrationController(
    IBusinessService businessService,
    IPlatformAuthService authService,
    UplivaDbContext db) : Controller
{

    [HttpGet]
    public IActionResult Register() => View(new BusinessRegistrationViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(BusinessRegistrationViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await authService.FindByEmailAsync(model.Email, cancellationToken) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account already exists with this email address.");
            return View(model);
        }

        var business = await businessService.RegisterAsync(model, cancellationToken);
        await authService.CreateBusinessOwnerAsync(model, business.Id, cancellationToken);

        return RedirectToAction(nameof(Pending), new { slug = business.Slug });
    }

    [HttpGet]
    public async Task<IActionResult> Pending(string slug, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
        if (business is null)
            return NotFound();

        return View(business);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Authorize(Roles = PlatformRoles.Admin)]
public class AdminDashboardController(
    IBusinessService businessService,
    IMarketingEngagementService marketingEngagement) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? businessId, CancellationToken cancellationToken)
    {
        var interestedLeads = await marketingEngagement.GetInterestedLeadsAsync(cancellationToken);
        var businesses = await businessService.GetAllAsync(cancellationToken);

        return View(new AdminBusinessListViewModel
        {
            InterestedLeads = interestedLeads,
            Businesses = businesses
                .Where(x => x.Status == BusinessStatuses.Approved)
                .OrderBy(x => x.Name)
                .ToList(),
            SelectedBusinessId = businessId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWhatsAppBusiness(int id, CancellationToken cancellationToken)
    {
        var business = await businessService.GetByIdAsync(id, cancellationToken);
        if (business is null) return NotFound();

        if (business.Status != BusinessStatuses.Approved)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "The business must be active before its WhatsApp Business setup can be configured.";
            return RedirectToAction(nameof(Index));
        }

        var existingSettings = await businessService.GetWhatsAppSettingsAsync(id, cancellationToken);
        if (existingSettings is null)
        {
            await businessService.CreateWhatsAppSettingsAsync(id, cancellationToken);
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"WhatsApp Business setup for {business.Name} was created.";
        }

        return RedirectToAction("Manage", "AdminBusinessIntegration", new { id });
    }
}

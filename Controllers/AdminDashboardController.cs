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
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index(int? businessId, CancellationToken cancellationToken)
    {
        var interestedLeads = await marketingEngagement.GetInterestedLeadsAsync(cancellationToken);
        var businesses = await businessService.GetAllAsync(cancellationToken);

        var activeBusinesses = businesses
            .Where(x => x.Status == BusinessStatuses.Approved)
            .OrderBy(x => x.Name)
            .ToList();

        // Never keep a deleted/stale business selection in the page model.
        var selectedBusinessId = businessId.HasValue && activeBusinesses.Any(x => x.Id == businessId.Value)
            ? businessId
            : null;

        return View(new AdminBusinessListViewModel
        {
            InterestedLeads = interestedLeads,
            Businesses = activeBusinesses,
            SelectedBusinessId = selectedBusinessId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBusiness(int id, CancellationToken cancellationToken)
    {
        try
        {
            var business = await businessService.GetByIdAsync(id, cancellationToken);
            if (business is null)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "The business no longer exists. The dashboard was refreshed.";
                return RedirectToAction(nameof(Index));
            }

            await businessService.DeleteAsync(id, cancellationToken);
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"{business.Name} was deleted successfully. It has been removed from the business list.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "The business could not be deleted. Check the error log for the correlation details.";
        }

        return RedirectToAction(nameof(Index));
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

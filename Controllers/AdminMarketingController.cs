using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

[Authorize(Roles = PlatformRoles.Admin)]
public class AdminMarketingController(IMarketingEngagementService engagement, IBusinessService businessService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        return View(await engagement.GetStatsAsync(q, page, 10, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> CreateBusiness(long leadId, CancellationToken cancellationToken)
    {
        var leads = await engagement.GetRecentLeadsAsync(5000, cancellationToken);
        var lead = leads.FirstOrDefault(x => x.Id == leadId);
        if (lead is null) return NotFound();
        if (lead.Status != MarketingLeadStatuses.Interested)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Only an Interested lead can be converted into a business.";
            return RedirectToAction(nameof(Index));
        }

        return View(new CreateBusinessFromLeadViewModel
        {
            LeadId = lead.Id,
            BusinessName = string.Empty,
            OwnerName = lead.Name,
            BusinessType = lead.BusinessType,
            WhatsAppNumber = lead.WhatsAppNumber,
            ServicePlan = lead.SelectedPlan
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBusiness(CreateBusinessFromLeadViewModel model, CancellationToken cancellationToken)
    {
        if (!BusinessServicePlans.All.Contains(model.ServicePlan, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.ServicePlan), "Select a valid Upliva plan.");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // The selected plan is kept on the lead as the source of truth. The
            // service validates the lead again before creating the business.
            var business = await businessService.CreateFromLeadAsync(model.LeadId, model.BusinessName, cancellationToken);
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"{business.Name} was created from the interested lead and is now active on the admin dashboard.";
            return RedirectToAction("Index", "AdminDashboard");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLead(
        long id,
        string status,
        string? selectedPlan,
        string? adminNotes,
        string? q,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await engagement.UpdateLeadAsync(id, status, selectedPlan, adminNotes, cancellationToken);
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Lead information updated successfully.";
        }
        catch (ArgumentException ex)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = ex.Message;
        }
        catch (KeyNotFoundException)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Lead could not be found.";
        }

        return RedirectToAction(nameof(Index), new { q, page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLead(long id, CancellationToken cancellationToken)
    {
        try
        {
            await engagement.DeleteLeadAsync(id, cancellationToken);
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Lead deleted successfully.";
        }
        catch (KeyNotFoundException)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Lead could not be found.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportLeadsCsv(CancellationToken cancellationToken)
    {
        var leads = await engagement.GetRecentLeadsAsync(5000, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("Name,BusinessType,WhatsAppNumber,Status,SelectedPlan,AdminNotes,Source,CreatedAtUtc,ContactedAtUtc,ConfirmedAtUtc,ConvertedAtUtc");

        foreach (var lead in leads)
        {
            sb.AppendLine(string.Join(',',
                Csv(lead.Name),
                Csv(lead.BusinessType),
                Csv(lead.WhatsAppNumber),
                Csv(lead.Status),
                Csv(MarketingLeadPlans.GetDisplayName(lead.SelectedPlan)),
                Csv(lead.AdminNotes),
                Csv(lead.Source),
                Csv(lead.CreatedAtUtc.ToString("O")),
                Csv(lead.ContactedAtUtc?.ToString("O") ?? string.Empty),
                Csv(lead.ConfirmedAtUtc?.ToString("O") ?? string.Empty),
                Csv(lead.ConvertedAtUtc?.ToString("O") ?? string.Empty)));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"uplivaai-business-leads-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}

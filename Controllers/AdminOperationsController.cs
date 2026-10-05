using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Controllers;

[Authorize(Roles = PlatformRoles.Admin)]
public class AdminOperationsController(UplivaDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? correlationId, int? businessId, string? integration, CancellationToken cancellationToken)
    {
        var query = db.IntegrationLogs.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).AsQueryable();
        if (!string.IsNullOrWhiteSpace(correlationId)) query = query.Where(x => x.CorrelationId == correlationId.Trim());
        if (businessId.HasValue) query = query.Where(x => x.BusinessId == businessId.Value);
        if (!string.IsNullOrWhiteSpace(integration)) query = query.Where(x => x.IntegrationName == integration.Trim());

        ViewBag.CorrelationId = correlationId;
        ViewBag.BusinessId = businessId;
        ViewBag.Integration = integration;
        return View(await query.Take(250).ToListAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Notifications(string? correlationId, int? businessId, string? status, CancellationToken cancellationToken)
    {
        var query = db.NotificationLogs.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).AsQueryable();
        if (!string.IsNullOrWhiteSpace(correlationId)) query = query.Where(x => x.CorrelationId == correlationId.Trim());
        if (businessId.HasValue) query = query.Where(x => x.BusinessId == businessId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());

        ViewBag.CorrelationId = correlationId;
        ViewBag.BusinessId = businessId;
        ViewBag.Status = status;
        return View(await query.Take(250).ToListAsync(cancellationToken));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Controllers;

[Authorize(Roles = PlatformRoles.Admin)]
public class AdminErrorLogsController(UplivaDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? correlationId, int? businessId, CancellationToken cancellationToken)
    {
        var query = db.ErrorLogs.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).AsQueryable();

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(x => x.CorrelationId == correlationId.Trim());

        if (businessId.HasValue)
            query = query.Where(x => x.BusinessId == businessId.Value);

        var logs = await query.Take(250).ToListAsync(cancellationToken);
        ViewBag.CorrelationId = correlationId;
        ViewBag.BusinessId = businessId;
        return View(logs);
    }
}

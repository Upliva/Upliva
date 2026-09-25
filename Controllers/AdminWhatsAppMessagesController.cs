using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Controllers;

[Authorize(Roles = PlatformRoles.Admin)]
public class AdminWhatsAppMessagesController(UplivaDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int businessId, string? q, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId, cancellationToken);
        if (business is null) return NotFound();

        var query = db.WhatsAppMessageLogs.AsNoTracking().Where(x => x.BusinessId == businessId);
        var term = q?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(x => x.CustomerPhoneNumber.Contains(term) || x.CustomerName.Contains(term) || x.MessageText.Contains(term));

        var messages = await query.OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync(cancellationToken);
        ViewBag.Business = business;
        ViewBag.Search = term;
        return View(messages);
    }
}

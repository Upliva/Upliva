using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Controllers;

[Authorize(Roles = "Admin,BusinessOwner")]
public class BusinessEnquiryController(UplivaDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id, CancellationToken cancellationToken)
    {
        var businessId = await ResolveBusinessIdAsync(id, cancellationToken);
        if (businessId is null) return Forbid();
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == businessId, cancellationToken);
        if (business is null) return NotFound();
        var enquiries = await db.BusinessEnquiries.AsNoTracking().Where(x => x.BusinessId == businessId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        ViewBag.Business = business;
        return View(enquiries);
    }

    private async Task<int?> ResolveBusinessIdAsync(int? requestedId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(PlatformRoles.Admin) && requestedId.HasValue) return await db.Businesses.AnyAsync(x => x.Id == requestedId.Value, cancellationToken) ? requestedId.Value : null;
        var claim = User.FindFirstValue("BusinessId");
        return int.TryParse(claim, out var id) ? id : null;
    }
}

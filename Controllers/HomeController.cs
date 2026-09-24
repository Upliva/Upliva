using Microsoft.AspNetCore.Mvc;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

public class HomeController(IMarketingEngagementService marketingEngagement) : Controller
{
    private const string VisitorCookie = "UplivaAI.VisitorId";

    [HttpGet]
    public async Task<IActionResult> Index(string? login = null, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var visitorId = GetOrCreateVisitorId();
        await marketingEngagement.RecordVisitAsync(visitorId, Request.Path, cancellationToken);

        ViewData["OpenLoginModal"] = login == "1";
        ViewData["LoginReturnUrl"] = returnUrl;
        ViewData["LoginError"] = TempData["LoginError"]?.ToString();
        return View("Marketing");
    }

    private string GetOrCreateVisitorId()
    {
        if (Request.Cookies.TryGetValue(VisitorCookie, out var existing) && !string.IsNullOrWhiteSpace(existing))
            return existing;

        var visitorId = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(VisitorCookie, visitorId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(365)
        });
        return visitorId;
    }
}

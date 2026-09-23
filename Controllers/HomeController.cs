using Microsoft.AspNetCore.Mvc;

namespace UplivaAI.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index(string? login = null, string? returnUrl = null)
    {
        // The public homepage is platform marketing only.
        // Individual business websites are exposed separately after admin approval and publishing.
        ViewData["OpenLoginModal"] = login == "1";
        ViewData["LoginReturnUrl"] = returnUrl;
        ViewData["LoginError"] = TempData["LoginError"]?.ToString();
        return View("Marketing");
    }
}

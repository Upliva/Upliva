using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UplivaResortBooking.Models;
using UplivaResortBooking.Services;

namespace UplivaResortBooking.Controllers;

public class AccountController(IPlatformAuthService authService) : Controller
{
    // Public login opens the homepage with the login modal.
    [AllowAnonymous]
    [HttpGet("/login", Name = "UplivaLogin")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(PlatformRoles.Admin))
                return RedirectToAction("Index", "AdminDashboard");

            if (User.IsInRole(PlatformRoles.BusinessOwner))
                return RedirectToAction("Index", "BusinessDashboard");

            Response.Cookies.Delete(".UplivaAI.Auth");
        }

        return RedirectToAction("Index", "Home", new { login = 1, returnUrl });
    }

    // Compatibility URL for users who directly type /Account/Login.
    [AllowAnonymous]
    [HttpGet("/Account/Login")]
    public IActionResult AccountLogin(string? returnUrl = null)
        => Login(returnUrl);

    // The modal posts directly to /login.
    [AllowAnonymous]
    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["LoginError"] = "Please enter a valid email and password.";
            return RedirectToAction("Index", "Home", new { login = 1, returnUrl = model.ReturnUrl });
        }

        var user = await authService.ValidateCredentialsAsync(model.Email, model.Password, cancellationToken);
        if (user is null)
        {
            TempData["LoginError"] = "Invalid email or password.";
            return RedirectToAction("Index", "Home", new { login = 1, returnUrl = model.ReturnUrl });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.BusinessId.HasValue)
            claims.Add(new Claim("BusinessId", user.BusinessId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return user.Role == PlatformRoles.Admin
            ? RedirectToAction("Index", "AdminDashboard")
            : RedirectToAction("Index", "BusinessDashboard");
    }

    [HttpPost("/Account/Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private IActionResult SignOutAndShowLogin(string? returnUrl)
    {
        // SignOutAsync cannot be awaited from this synchronous GET without changing the action.
        // Expire the cookie directly so the next request is clean.
        Response.Cookies.Delete(".UplivaAI.Auth");
        return View("Login", new LoginViewModel { ReturnUrl = returnUrl });
    }
}

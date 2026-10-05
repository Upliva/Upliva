using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

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
    [EnableRateLimiting("login")]
    public async Task<IActionResult> LoginPost(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["LoginError"] = "Please enter your User ID / mobile number and password.";
            return RedirectToAction("Index", "Home", new { login = 1, returnUrl = model.ReturnUrl });
        }

        var user = await authService.ValidateCredentialsAsync(model.Email, model.Password, cancellationToken);
        if (user is null)
        {
            TempData["LoginError"] = "Invalid User ID / mobile number or password.";
            return RedirectToAction("Index", "Home", new { login = 1, returnUrl = model.ReturnUrl });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("MustChangePassword", user.MustChangePassword ? "true" : "false")
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

        if (user.Role == PlatformRoles.BusinessOwner && user.MustChangePassword)
            return RedirectToAction(nameof(ChangePassword));

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return user.Role == PlatformRoles.Admin
            ? RedirectToAction("Index", "AdminDashboard")
            : RedirectToAction("Index", "BusinessDashboard");
    }

    [Authorize]
    [HttpGet("/account/change-password")]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost("/account/change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();

        var user = await authService.FindByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive) return Forbid();
        if (!authService.VerifyPassword(user, model.CurrentPassword))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
            return View(model);
        }

        await authService.ChangePasswordAsync(user, model.NewPassword, cancellationToken);
        var identity = User.Identity as ClaimsIdentity;
        var passwordClaim = identity?.FindFirst("MustChangePassword");
        if (identity is not null && passwordClaim is not null)
        {
            identity.RemoveClaim(passwordClaim);
            identity.AddClaim(new Claim("MustChangePassword", "false"));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }
        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Your password was changed successfully.";
        return RedirectToAction("Index", "BusinessDashboard");
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

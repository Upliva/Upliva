using System.Security.Claims;
using UplivaAI.Models;

namespace UplivaAI.Middleware;

public sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.IsInRole(PlatformRoles.BusinessOwner) &&
            string.Equals(context.User.FindFirstValue("MustChangePassword"), "true", StringComparison.OrdinalIgnoreCase))
        {
            var path = context.Request.Path;
            var allowed = path.StartsWithSegments("/account/change-password") ||
                          path.StartsWithSegments("/Account/Logout") ||
                          path.StartsWithSegments("/css") ||
                          path.StartsWithSegments("/js") ||
                          path.StartsWithSegments("/images") ||
                          path.StartsWithSegments("/favicon");
            if (!allowed)
            {
                context.Response.Redirect("/account/change-password");
                return;
            }
        }

        await next(context);
    }
}

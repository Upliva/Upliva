using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public static class PlatformAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UplivaDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<PlatformUser>>();

        var email = configuration["Admin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var admin = await db.PlatformUsers.FirstOrDefaultAsync(x => x.Email == email);

        if (admin is null)
        {
            admin = new PlatformUser
            {
                FullName = configuration["Admin:Name"]?.Trim() ?? "UplivaAI Administrator",
                Email = email,
                Role = PlatformRoles.Admin,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            admin.PasswordHash = passwordHasher.HashPassword(admin, password);
            db.PlatformUsers.Add(admin);
            await db.SaveChangesAsync();
            return;
        }

        // In local development, User Secrets are the source of truth for the initial
        // administrator credentials. If the configured password changes, update the hash
        // so the user can log in without manually editing the database.
        admin.FullName = configuration["Admin:Name"]?.Trim() ?? admin.FullName;
        admin.Role = PlatformRoles.Admin;
        admin.IsActive = true;

        var verification = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
            admin.PasswordHash = passwordHasher.HashPassword(admin, password);

        await db.SaveChangesAsync();
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public class PlatformAuthService(
    UplivaDbContext db,
    IPasswordHasher<PlatformUser> passwordHasher) : IPlatformAuthService
{
    public async Task<PlatformUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var identifier = email?.Trim() ?? string.Empty;
        var normalizedEmail = identifier.ToLowerInvariant();
        var normalizedPhone = BusinessRegistrationInputHelper.NormalizePhone(identifier);

        return await db.PlatformUsers.FirstOrDefaultAsync(
            x => x.Email == normalizedEmail ||
                 (!string.IsNullOrWhiteSpace(normalizedPhone) && x.PhoneNumber == normalizedPhone), cancellationToken);
    }

    public async Task<PlatformUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await FindByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive)
            return null;

        if (user.Role == PlatformRoles.BusinessOwner && user.BusinessId.HasValue)
        {
            var business = await db.Businesses.FirstOrDefaultAsync(x => x.Id == user.BusinessId.Value, cancellationToken);
            if (business is null || business.Status != BusinessStatuses.Approved)
                return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<PlatformUser> CreateBusinessOwnerAsync(
        BusinessRegistrationViewModel model,
        int businessId,
        CancellationToken cancellationToken = default)
    {
        var user = new PlatformUser
        {
            FullName = model.OwnerName.Trim(),
            Email = string.IsNullOrWhiteSpace(model.Email)
                ? BusinessRegistrationInputHelper.BuildInternalEmail(model.WhatsAppNumber)
                : model.Email.Trim().ToLowerInvariant(),
            PhoneNumber = BusinessRegistrationInputHelper.NormalizePhone(model.WhatsAppNumber),
            Role = PlatformRoles.BusinessOwner,
            BusinessId = businessId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, model.Password);
        db.PlatformUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }
}

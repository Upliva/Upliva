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

        var phoneVariants = BusinessInputRules.GetWhatsAppVariants(normalizedPhone);
        return await db.PlatformUsers.FirstOrDefaultAsync(
            x => x.Email == normalizedEmail ||
                 (phoneVariants.Count > 0 && phoneVariants.Contains(x.PhoneNumber)), cancellationToken);
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

    public Task<PlatformUser?> FindByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.PlatformUsers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public bool VerifyPassword(PlatformUser user, string password) =>
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;

    public async Task ChangePasswordAsync(PlatformUser user, string newPassword, CancellationToken cancellationToken = default)
    {
        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        await db.SaveChangesAsync(cancellationToken);
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
    public async Task<PlatformUser> CreateBusinessOwnerAsync(
        Business business,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        var phone = BusinessRegistrationInputHelper.NormalizePhone(business.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone))
            throw new InvalidOperationException("A business owner mobile number is required before the owner account can be created.");

        var existing = await db.PlatformUsers.FirstOrDefaultAsync(x => x.PhoneNumber == phone && x.Role == PlatformRoles.BusinessOwner, cancellationToken);
        if (existing is not null)
        {
            if (existing.BusinessId != business.Id)
                throw new InvalidOperationException("This mobile number is already assigned to another business owner account.");
            return existing;
        }

        var user = new PlatformUser
        {
            FullName = string.IsNullOrWhiteSpace(business.OwnerName) ? business.Name : business.OwnerName.Trim(),
            Email = string.IsNullOrWhiteSpace(business.Email) ? BusinessRegistrationInputHelper.BuildInternalEmail(phone) : business.Email.Trim().ToLowerInvariant(),
            PhoneNumber = phone, Role = PlatformRoles.BusinessOwner, BusinessId = business.Id, IsActive = true, MustChangePassword = true, CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, temporaryPassword);
        db.PlatformUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

}

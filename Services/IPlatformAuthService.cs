using UplivaAI.Models;

namespace UplivaAI.Services;

public interface IPlatformAuthService
{
    Task<PlatformUser?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<PlatformUser> CreateBusinessOwnerAsync(BusinessRegistrationViewModel model, int businessId, CancellationToken cancellationToken = default);
    Task<PlatformUser> CreateBusinessOwnerAsync(Business business, string temporaryPassword, CancellationToken cancellationToken = default);
    Task<PlatformUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<PlatformUser?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    bool VerifyPassword(PlatformUser user, string password);
    Task ChangePasswordAsync(PlatformUser user, string newPassword, CancellationToken cancellationToken = default);
}

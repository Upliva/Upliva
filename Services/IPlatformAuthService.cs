using UplivaAI.Models;

namespace UplivaAI.Services;

public interface IPlatformAuthService
{
    Task<PlatformUser?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<PlatformUser> CreateBusinessOwnerAsync(BusinessRegistrationViewModel model, int businessId, CancellationToken cancellationToken = default);
    Task<PlatformUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}

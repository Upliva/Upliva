using UplivaAI.Models;

namespace UplivaAI.Services;

public interface IBusinessService
{
    Task<Business?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Business?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Business>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Business> RegisterAsync(BusinessRegistrationViewModel model, CancellationToken cancellationToken = default);
    Task ApproveAsync(int id, CancellationToken cancellationToken = default);
    Task RejectAsync(int id, CancellationToken cancellationToken = default);
    Task PublishAsync(int id, CancellationToken cancellationToken = default);
    Task UnpublishAsync(int id, CancellationToken cancellationToken = default);
}

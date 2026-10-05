using UplivaAI.Models;

namespace UplivaAI.Services;

public interface INotificationService
{
    Task QueueBusinessEnquiryEmailAsync(BusinessEnquiry enquiry, Business business, CancellationToken cancellationToken = default);
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}

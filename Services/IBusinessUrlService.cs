using UplivaAI.Models;

namespace UplivaAI.Services;

public interface IBusinessUrlService
{
    string GetPublicBaseUrl(Business business);
    string GetWebsiteUrl(Business business);
}

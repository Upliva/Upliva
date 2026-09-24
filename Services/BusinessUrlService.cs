using UplivaAI.Models;

namespace UplivaAI.Services;

public sealed class BusinessUrlService(IConfiguration configuration) : IBusinessUrlService
{
    public string GetPublicBaseUrl(Business business)
    {
        if (business.IsPublished && business.IsCustomDomainEnabled && business.IsCustomDomainVerified &&
            !string.IsNullOrWhiteSpace(business.CustomDomain))
        {
            return $"https://{business.CustomDomain.Trim().TrimEnd('/') }";
        }

        return (configuration["Platform:PublicBaseUrl"] ?? "https://localhost:7248").TrimEnd('/');
    }

    public string GetWebsiteUrl(Business business)
    {
        if (business.IsPublished && business.IsCustomDomainEnabled && business.IsCustomDomainVerified &&
            !string.IsNullOrWhiteSpace(business.CustomDomain))
            return GetPublicBaseUrl(business);

        return $"{GetPublicBaseUrl(business)}/business/{business.Slug}";
    }
}

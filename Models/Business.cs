using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public static class BusinessServicePlans
{
    public const string WhatsAppOnly = MarketingLeadPlans.WhatsAppOnly;
    public const string WhatsAppWebsite = MarketingLeadPlans.WhatsAppWebsite;
    public const string WhatsAppWebsiteEnquiry = MarketingLeadPlans.WhatsAppWebsiteEnquiry;

    public static IReadOnlyList<string> All { get; } = new[]
    {
        WhatsAppOnly, WhatsAppWebsite, WhatsAppWebsiteEnquiry
    };

    public static string GetDisplayName(string? plan) => MarketingLeadPlans.GetDisplayName(plan);
}

public static class BusinessStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Suspended = "Suspended";
}

public class Business
{
    public int Id { get; set; }

    [Required, MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string BusinessType { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Status { get; set; } = BusinessStatuses.Pending;

    [MaxLength(40)]
    public string ServicePlan { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    [MaxLength(150)]
    public string OwnerName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(30)]
    public string WhatsAppNumber { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Country { get; set; } = "India";

    [MaxLength(500)]
    public string BusinessHours { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Tagline { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string LogoUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string HeroImageUrl { get; set; } = string.Empty;

    [MaxLength(253)]
    public string? CustomDomain { get; set; }

    public bool IsCustomDomainEnabled { get; set; }
    public bool IsCustomDomainVerified { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}

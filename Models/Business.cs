using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public static class BusinessServicePlans
{
    public const string WhatsAppSms = MarketingLeadPlans.WhatsAppSms;
    public const string WhatsAppEnquiryFollowUp = MarketingLeadPlans.WhatsAppEnquiryFollowUp;

    public static IReadOnlyList<string> All { get; } = new[]
    {
        WhatsAppSms,
        WhatsAppEnquiryFollowUp
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

/// <summary>
/// Operational business record for the current WhatsApp-first MVP.
/// Website/custom-domain fields are intentionally not part of this schema yet.
/// </summary>
public class Business
{
    public int Id { get; set; }

    [Required, MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string BusinessType { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string CatalogTemplateKey { get; set; } = "generic";

    [Required, MaxLength(40)]
    public string Status { get; set; } = BusinessStatuses.Pending;

    [MaxLength(40)]
    public string ServicePlan { get; set; } = string.Empty;

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

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
}

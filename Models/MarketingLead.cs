using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public static class MarketingLeadStatuses
{
    // Initial-phase lead outcome options shown to the admin.
    public const string Interested = "Interested";
    public const string NotInterested = "Not Interested";

    // Legacy values are retained for compatibility with existing reporting/model properties.
    // They are intentionally not exposed in All and cannot be selected from the admin dropdown.
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Confirmed = "Confirmed";
    public const string Converted = "Converted";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Interested, NotInterested
    };
}

/// <summary>
/// Plans are selected by the Upliva team after the initial phone call.
/// The public registration/chatbot never asks the visitor to choose a plan.
/// </summary>
public static class MarketingLeadPlans
{
    public const string WhatsAppOnly = "WhatsAppOnly";
    public const string WhatsAppWebsite = "WhatsAppWebsite";
    public const string WhatsAppWebsiteEnquiry = "WhatsAppWebsiteEnquiry";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        WhatsAppOnly,
        WhatsAppWebsite,
        WhatsAppWebsiteEnquiry
    };

    public static string GetDisplayName(string? plan) => plan switch
    {
        WhatsAppOnly => "WhatsApp only",
        WhatsAppWebsite => "WhatsApp + Website",
        WhatsAppWebsiteEnquiry => "WhatsApp + Website + Business Enquiry",
        _ => "Not selected"
    };
}

/// <summary>
/// A prospective business owner captured before a real Business account is created.
/// The lead is manually qualified by the Upliva team before conversion.
/// </summary>
public class MarketingLead
{
    public long Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string BusinessType { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string WhatsAppNumber { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Source { get; set; } = "MarketingChatbot";

    [Required, MaxLength(30)]
    public string Status { get; set; } = MarketingLeadStatuses.Interested;

    /// <summary>Plan selected after the phone conversation. Empty until the team confirms it.</summary>
    [MaxLength(40)]
    public string SelectedPlan { get; set; } = string.Empty;

    /// <summary>Internal follow-up notes. Never collected from the public chatbot.</summary>
    [MaxLength(1000)]
    public string AdminNotes { get; set; } = string.Empty;

    [MaxLength(100)]
    public string VisitorId { get; set; } = string.Empty;

    /// <summary>Set only after the team creates a real Business.</summary>
    public int? ConvertedBusinessId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ContactedAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? ConvertedAtUtc { get; set; }
}

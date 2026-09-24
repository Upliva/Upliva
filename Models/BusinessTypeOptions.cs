namespace UplivaAI.Models;

/// <summary>
/// Supported business categories shown during registration and business configuration.
/// BusinessType remains a string in the database so adding a category does not require an EF migration.
/// </summary>
public static class BusinessTypeOptions
{
    public static IReadOnlyList<string> All { get; } = new[]
    {
        "Mobile Shop",
        "Electronics Store",
        "Retail",
        "Grocery",
        "Hardware",
        "Furniture",
        "Salon & Beauty",
        "Restaurant",
        "Pathology / Diagnostic",
        "School / Education",
        "Transportation",
        "Real Estate",
        "Banquet / Hotel / Resort",
        "Rental Property",
        "Other"
    };
}

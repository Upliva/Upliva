using System.Text;

namespace UplivaAI.Services;

/// <summary>
/// Centralizes the MVP rule that business onboarding fields are optional.
/// The only required catalog field is Product Name. WhatsApp is optional, but
/// when supplied it is normalized and checked for duplicates by the caller.
/// </summary>
public static class BusinessInputRules
{
    public const string DefaultBusinessName = "New Upliva Business";
    public const string DefaultBusinessType = "Other";
    public const string DefaultPlan = "WhatsAppSms";

    public static string Clean(string? value) => value?.Trim() ?? string.Empty;

    public static string NormalizeWhatsApp(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 10 && digits[0] is >= '6' and <= '9')
            return "91" + digits;
        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
            return digits;
        return digits;
    }

    public static IReadOnlyList<string> GetWhatsAppVariants(string? value)
    {
        var normalized = NormalizeWhatsApp(value);
        if (string.IsNullOrWhiteSpace(normalized)) return Array.Empty<string>();
        if (normalized.Length == 12 && normalized.StartsWith("91", StringComparison.Ordinal))
            return new[] { normalized, normalized[2..] };
        return new[] { normalized };
    }

    public static bool IsOptionalWhatsAppValid(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return true;
        return (digits.Length == 10 && digits[0] is >= '6' and <= '9')
            || (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal) && digits[2] is >= '6' and <= '9');
    }

    public static string ResolveBusinessName(string? value, string? fallback = null)
    {
        var name = Clean(value);
        if (!string.IsNullOrWhiteSpace(name)) return name.Length > 180 ? name[..180] : name;
        var candidate = Clean(fallback);
        return string.IsNullOrWhiteSpace(candidate)
            ? DefaultBusinessName
            : candidate.Length > 180 ? candidate[..180] : candidate;
    }

    public static string ResolveBusinessType(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DefaultBusinessType : Clean(value);

    public static string ResolvePlan(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DefaultPlan : Clean(value);
}

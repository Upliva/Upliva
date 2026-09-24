namespace UplivaAI.Services;

public static class BusinessRegistrationInputHelper
{
    public static string NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value.Where(char.IsDigit).ToArray());
    }


    /// <summary>
    /// Normalizes the public India lead-capture phone input. Visitors enter only
    /// the familiar 10-digit Indian mobile number; the country code 91 is added
    /// internally for WhatsApp links/integrations.
    /// </summary>
    public static string NormalizeIndianLeadWhatsAppNumber(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length == 10 && IsValidIndianLeadWhatsAppNumber(digits))
            return $"91{digits}";

        // Keep an already-normalized Indian number unchanged. This makes the
        // service safe for internal callers while the public forms still accept
        // only the simple 10-digit format.
        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal) &&
            IsValidIndianLeadWhatsAppNumber(digits[2..]))
            return digits;

        return digits;
    }

    public static bool IsValidIndianLeadWhatsAppNumber(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == 10 && digits[0] is >= '6' and <= '9';
    }

    public static bool IsValidWhatsAppNumber(string? value)
    {
        var normalized = NormalizePhone(value);
        return normalized.Length is >= 10 and <= 15;
    }

    public static string BuildInternalEmail(string whatsappNumber)
    {
        // PlatformUser.Email is a legacy required/unique database field.
        // When a business does not provide an email, keep the field unique without
        // exposing a fake email to the customer. Login still works with WhatsApp.
        return $"wa-{whatsappNumber}@accounts.upliva.local";
    }
}

namespace UplivaAI.Models;

/// <summary>
/// Non-secret Gupshup configuration. The API key must be supplied through
/// User Secrets, environment variables, Azure App Service settings, or Key Vault.
/// </summary>
public class GupshupSettings
{
    public string BaseUrl { get; set; } = "https://api.gupshup.io";
    public string AppName { get; set; } = string.Empty;
    public string SourceNumber { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}

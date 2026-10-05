namespace UplivaAI.Services;

public sealed class NotificationOptions
{
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "UplivaAI";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public int RetryMaxAttempts { get; set; } = 3;
    public int RetryDelayMinutes { get; set; } = 5;
}

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace UplivaAI.Services;

public sealed class SmtpEmailService(IOptions<NotificationOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly NotificationOptions _options = options.Value;

    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost) || string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("Email notifications are not configured. Set Notifications:SmtpHost and Notifications:FromEmail in User Secrets/environment variables.");

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(recipient));

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
            client.Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword);

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Business enquiry email sent to {Recipient}", MaskEmail(recipient));
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "[masked]";
        return email[0] + "***" + email[at..];
    }
}

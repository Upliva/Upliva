using System.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Middleware;

namespace UplivaAI.Services;

public sealed class NotificationService(
    UplivaDbContext db,
    IEmailService emailService,
    IOptions<NotificationOptions> options,
    IHttpContextAccessor httpContextAccessor,
    IIntegrationLogService integrationLogService,
    ILogger<NotificationService> logger) : INotificationService
{
    private readonly NotificationOptions _options = options.Value;

    public async Task QueueBusinessEnquiryEmailAsync(BusinessEnquiry enquiry, Business business, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(business.Email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(business.Email))
        {
            logger.LogInformation("Business has no valid owner email. EnquiryId={EnquiryId}, BusinessId={BusinessId}", enquiry.Id, business.Id);
            return;
        }

        var correlationId = httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
            ?? Guid.NewGuid().ToString("N");

        db.NotificationLogs.Add(new NotificationLog
        {
            BusinessId = business.Id,
            BusinessEnquiryId = enquiry.Id,
            Channel = "Email",
            Status = NotificationStatuses.Pending,
            Recipient = business.Email.Trim(),
            Subject = $"New customer enquiry - {business.Name}",
            CorrelationId = correlationId,
            NextAttemptAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var logs = await db.NotificationLogs
            .Where(x => x.Status == NotificationStatuses.Pending && x.NextAttemptAtUtc <= now && x.Attempts < Math.Max(1, _options.RetryMaxAttempts))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
        {
            var enquiry = log.BusinessEnquiryId.HasValue
                ? await db.BusinessEnquiries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == log.BusinessEnquiryId.Value, cancellationToken)
                : null;
            var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == log.BusinessId, cancellationToken);
            if (enquiry is null || business is null)
            {
                log.Status = NotificationStatuses.Failed;
                log.LastError = "Related business or enquiry was not found.";
                log.LastAttemptAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            var attempt = log.Attempts + 1;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var html = BuildHtml(enquiry, business);
                await emailService.SendAsync(log.Recipient, log.Subject, html, cancellationToken);
                log.Attempts = attempt;
                log.LastAttemptAtUtc = DateTime.UtcNow;
                log.Status = NotificationStatuses.Sent;
                log.LastError = string.Empty;
                log.NextAttemptAtUtc = null;
            }
            catch (Exception ex)
            {
                log.Attempts = attempt;
                log.LastAttemptAtUtc = DateTime.UtcNow;
                log.LastError = ex.Message.Length <= 2000 ? ex.Message : ex.Message[..2000];
                log.Status = attempt >= Math.Max(1, _options.RetryMaxAttempts) ? NotificationStatuses.Failed : NotificationStatuses.Pending;
                log.NextAttemptAtUtc = log.Status == NotificationStatuses.Pending
                    ? DateTime.UtcNow.AddMinutes(Math.Max(1, _options.RetryDelayMinutes) * attempt)
                    : null;
            }
            finally
            {
                stopwatch.Stop();
                await db.SaveChangesAsync(cancellationToken);
                await integrationLogService.WriteAsync(log.BusinessId, "Email", "BusinessEnquiryNotification", log.Status == NotificationStatuses.Sent ? "Success" : "Failed", log.CorrelationId, stopwatch.ElapsedMilliseconds, attempt, errorMessage: log.LastError, cancellationToken: cancellationToken);
                logger.LogInformation("Notification processed. Id={NotificationId}, Status={Status}, Attempt={Attempt}, DurationMs={DurationMs}", log.Id, log.Status, attempt, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private static string BuildHtml(BusinessEnquiry enquiry, Business business) => $"""
        <h2>New enquiry for {WebUtility.HtmlEncode(business.Name)}</h2>
        <p><strong>Customer:</strong> {WebUtility.HtmlEncode(enquiry.Name)}</p>
        <p><strong>Phone:</strong> {WebUtility.HtmlEncode(enquiry.PhoneNumber)}</p>
        <p><strong>Email:</strong> {WebUtility.HtmlEncode(enquiry.Email)}</p>
        <p><strong>Message:</strong><br/>{WebUtility.HtmlEncode(enquiry.Message).Replace("\n", "<br/>")}</p>
        <p><strong>Received:</strong> {enquiry.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC</p>
        <p>Log in to Upliva to manage this enquiry.</p>
        """;
}

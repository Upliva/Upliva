using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

/// <summary>
/// Server-side adapter for the Gupshup WhatsApp API.
/// Gupshup template messages are sent from UplivaAI's server, never from browser JavaScript.
/// </summary>
public sealed class GupshupWhatsAppService(
    HttpClient httpClient,
    IOptions<GupshupSettings> options,
    UplivaDbContext db,
    ILogger<GupshupWhatsAppService> logger) : IGupshupWhatsAppService
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{([^{}]+)\}\}", RegexOptions.Compiled);
    private readonly GupshupSettings _settings = options.Value;

    public async Task<(bool Success, string Message, string? MessageId)> SendTemplateAsync(
        int businessId,
        WhatsAppTemplateConfiguration template,
        string destination,
        IReadOnlyList<string> parameters,
        CancellationToken cancellationToken = default)
    {
        if (businessId <= 0)
            return (false, "Select a business before sending the WhatsApp template.", null);

        var businessExists = await db.Businesses.AsNoTracking()
            .AnyAsync(x => x.Id == businessId && x.Status == BusinessStatuses.Approved, cancellationToken);
        if (!businessExists)
            return (false, "The selected business is not approved or no longer exists.", null);

        if (!string.Equals(template.Provider, "Gupshup", StringComparison.OrdinalIgnoreCase))
            return (false, "This send screen currently supports Gupshup templates only.", null);

        var apiKey = Resolve("ApiKey");
        var appName = Resolve("AppName");
        var sourceNumber = NormalizePhone(Resolve("SourceNumber"));
        var baseUrl = (Resolve("BaseUrl") ?? "https://api.gupshup.io").TrimEnd('/');
        var templateId = template.ProviderTemplateName.Trim();
        var recipient = NormalizePhone(destination);

        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, "Gupshup API key is not configured. Add WhatsApp:Gupshup:ApiKey to User Secrets.", null);
        if (string.IsNullOrWhiteSpace(appName))
            return (false, "Gupshup App Name is not configured. Add WhatsApp:Gupshup:AppName to User Secrets.", null);
        if (string.IsNullOrWhiteSpace(sourceNumber))
            return (false, "Gupshup source WhatsApp number is not configured. Add WhatsApp:Gupshup:SourceNumber to User Secrets.", null);
        if (string.IsNullOrWhiteSpace(templateId))
            return (false, "The Gupshup Template ID is missing from the saved template.", null);
        if (string.IsNullOrWhiteSpace(recipient))
            return (false, "Enter a valid recipient WhatsApp number with country code.", null);

        var expectedParameters = ExtractPlaceholders(template.BodyText);
        if (parameters.Count != expectedParameters.Count)
            return (false, $"Template '{template.Name}' expects {expectedParameters.Count} parameter(s), but {parameters.Count} were supplied.", null);

        if (parameters.Any(string.IsNullOrWhiteSpace))
            return (false, "Every template parameter must have a value.", null);

        var renderedBody = RenderBody(template.BodyText, expectedParameters, parameters);
        var correlationId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var templateJson = JsonSerializer.Serialize(new
            {
                id = templateId,
                @params = parameters
            });

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl}/wa/api/v1/template/msg");

            request.Headers.TryAddWithoutValidation("apikey", apiKey);
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["channel"] = "whatsapp",
                ["source"] = sourceNumber,
                ["destination"] = recipient,
                ["src.name"] = appName,
                ["template"] = templateJson
            });

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            string? messageId = null;
            string message;
            try
            {
                using var json = JsonDocument.Parse(responseBody);
                var root = json.RootElement;
                messageId = root.TryGetProperty("messageId", out var id) ? id.GetString() : null;
                message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? responseBody : responseBody;
            }
            catch
            {
                message = responseBody;
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = string.IsNullOrWhiteSpace(message) ? $"Gupshup returned HTTP {(int)response.StatusCode}." : message;
                logger.LogWarning(
                    "Gupshup template send failed. BusinessId={BusinessId}, Status={Status}, Template={TemplateName}, CorrelationId={CorrelationId}",
                    businessId, (int)response.StatusCode, template.Name, correlationId);

                await LogOutboundAsync(businessId, recipient, template, renderedBody, messageId, "Failed", error, cancellationToken);
                return (false, $"Gupshup rejected the message ({(int)response.StatusCode}). {error}", messageId);
            }

            var submittedStatus = string.IsNullOrWhiteSpace(message) ? "Gupshup accepted the request." : message;
            logger.LogInformation(
                "Gupshup template submitted. BusinessId={BusinessId}, Template={TemplateName}, MessageId={MessageId}, DurationMs={DurationMs}, CorrelationId={CorrelationId}",
                businessId, template.Name, messageId, stopwatch.ElapsedMilliseconds, correlationId);

            await LogOutboundAsync(businessId, recipient, template, renderedBody, messageId, "Submitted", string.Empty, cancellationToken);
            return (true, string.IsNullOrWhiteSpace(messageId) ? submittedStatus : $"Gupshup accepted the message. Message ID: {messageId}", messageId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Gupshup template send exception. BusinessId={BusinessId}, Template={TemplateName}, DurationMs={DurationMs}, CorrelationId={CorrelationId}",
                businessId, template.Name, stopwatch.ElapsedMilliseconds, correlationId);

            await LogOutboundAsync(businessId, recipient, template, renderedBody, null, "Failed", ex.Message, cancellationToken);
            return (false, "Gupshup could not be reached. Check the application logs and Gupshup configuration.", null);
        }
    }

    private string? Resolve(string name)
    {
        // The options object is populated from configuration, including User Secrets.
        return name switch
        {
            "ApiKey" => _settings.ApiKey?.Trim(),
            "AppName" => _settings.AppName?.Trim(),
            "SourceNumber" => _settings.SourceNumber?.Trim(),
            "BaseUrl" => _settings.BaseUrl?.Trim(),
            _ => null
        };
    }

    private async Task LogOutboundAsync(
        int businessId,
        string destination,
        WhatsAppTemplateConfiguration template,
        string renderedBody,
        string? messageId,
        string status,
        string error,
        CancellationToken cancellationToken)
    {
        try
        {
            db.WhatsAppMessageLogs.Add(new WhatsAppMessageLog
            {
                BusinessId = businessId,
                Direction = WhatsAppMessageDirections.Outbound,
                MessageType = "template",
                CustomerPhoneNumber = destination,
                ExternalMessageId = messageId ?? string.Empty,
                MessageText = renderedBody,
                DeliveryStatus = status,
                ErrorMessage = error.Length > 2000 ? error[..2000] : error
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not persist Gupshup outbound message log. BusinessId={BusinessId}", businessId);
        }
    }

    public static IReadOnlyList<string> ExtractPlaceholders(string body)
        => PlaceholderRegex.Matches(body ?? string.Empty)
            .Select(m => m.Groups[1].Value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string RenderBody(string body, IReadOnlyList<string> names, IReadOnlyList<string> values)
    {
        var rendered = body ?? string.Empty;
        for (var i = 0; i < Math.Min(names.Count, values.Count); i++)
        {
            var token = "{{" + names[i] + "}}";
            rendered = rendered.Replace(token, values[i] ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        return rendered;
    }

    private static string NormalizePhone(string value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits;
    }
}

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

/// <summary>
/// Receives both the existing Meta webhook shape and Gupshup v2 callbacks.
/// This endpoint is intentionally anonymous because Gupshup/Meta are external callers;
/// when WebhookSecret is configured, the matching custom header is required.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("webhooks/whatsapp")]
public class WhatsAppWebhookController(
    IOptions<WhatsAppSettings> options,
    IOptions<GupshupSettings> gupshupOptions,
    UplivaDbContext db,
    IWhatsAppFlowService flowService,
    ILogger<WhatsAppWebhookController> logger) : ControllerBase
{
    private readonly WhatsAppSettings _settings = options.Value;
    private readonly GupshupSettings _gupshup = gupshupOptions.Value;

    [HttpGet]
    public async Task<IActionResult> Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        logger.LogInformation("WhatsApp webhook verification request received.");

        var businessTokenMatches = !string.IsNullOrWhiteSpace(verifyToken) &&
            await db.BusinessWhatsAppSettings.AsNoTracking()
                .AnyAsync(x => x.IsEnabled && x.WebhookVerifyToken == verifyToken, HttpContext.RequestAborted);

        var configuredWebhookToken = string.IsNullOrWhiteSpace(_settings.WebhookVerifyToken)
            ? null
            : _settings.WebhookVerifyToken.Trim();

        if (mode == "subscribe" &&
            !string.IsNullOrWhiteSpace(verifyToken) &&
            ((configuredWebhookToken is not null && verifyToken == configuredWebhookToken) || businessTokenMatches))
        {
            logger.LogInformation("WhatsApp webhook verification successful.");
            return Content(challenge ?? string.Empty);
        }

        logger.LogWarning("WhatsApp webhook verification failed.");
        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        if (!WebhookSecretMatches())
            return Unauthorized();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(body))
            return Ok();

        logger.LogInformation("WhatsApp webhook POST received. PayloadLength={Length}", body.Length);

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            // Gupshup v2 callback format.
            if (root.TryGetProperty("version", out var version) && version.ToString() == "2" &&
                root.TryGetProperty("type", out var typeElement))
            {
                var eventType = typeElement.GetString();
                await HandleGupshupEventAsync(root, eventType, cancellationToken);
                return Ok();
            }

            // Existing Meta Cloud API webhook format.
            var payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var messages = payload?.Entry?
                .SelectMany(x => x.Changes ?? [])
                .SelectMany(x => x.Value?.Messages ?? [])
                .ToList() ?? [];

            foreach (var message in messages)
            {
                var value = payload!.Entry!
                    .SelectMany(x => x.Changes ?? [])
                    .FirstOrDefault(x => x.Value?.Messages?.Any(m => m.Id == message.Id) == true)
                    ?.Value;

                var contact = value?.Contacts?.FirstOrDefault(x => x.WaId == message.From);
                var selectionId = message.Interactive?.ButtonReply?.Id
                                  ?? message.Interactive?.ListReply?.Id;

                await flowService.HandleIncomingMessageAsync(
                    message.From ?? string.Empty,
                    contact?.Profile?.Name,
                    message.Type,
                    message.Text?.Body,
                    selectionId,
                    value?.Metadata?.PhoneNumberId,
                    message.Id,
                    cancellationToken);
            }

            return Ok();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid WhatsApp webhook JSON.");
            return BadRequest();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while processing WhatsApp webhook.");
            return StatusCode(500);
        }
    }

    private bool WebhookSecretMatches()
    {
        // Development can leave this empty. For production, configure the same secret
        // as a custom Gupshup webhook header and enable "Includes headers" in Gupshup.
        var expected = _gupshup.WebhookSecret?.Trim();
        if (string.IsNullOrWhiteSpace(expected))
            return true;

        var supplied = Request.Headers["X-Upliva-Webhook-Secret"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(supplied) &&
               CryptographicEquals(supplied, expected);
    }

    private async Task HandleGupshupEventAsync(
        JsonElement root,
        string? eventType,
        CancellationToken cancellationToken)
    {
        if (string.Equals(eventType, "message-event", StringComparison.OrdinalIgnoreCase))
        {
            await HandleGupshupMessageEventAsync(root, cancellationToken);
            return;
        }

        if (string.Equals(eventType, "message", StringComparison.OrdinalIgnoreCase))
        {
            // Inbound Gupshup messages do not contain a Meta Phone Number ID. The
            // current business flow remains Meta-compatible; for Gupshup we acknowledge
            // and log the inbound event when a business can be identified by the source
            // number. This avoids inventing a business mapping for a shared platform number.
            await HandleGupshupInboundMessageAsync(root, cancellationToken);
            return;
        }

        logger.LogInformation("Gupshup webhook event received and acknowledged. EventType={EventType}", eventType);
    }

    private async Task HandleGupshupMessageEventAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("payload", out var payload))
            return;

        var eventStatus = payload.TryGetProperty("type", out var type) ? type.GetString() : null;
        var messageId = payload.TryGetProperty("id", out var id) ? id.GetString() : null;
        var gupshupId = payload.TryGetProperty("gsId", out var gsId) ? gsId.GetString() : null;
        var destination = payload.TryGetProperty("destination", out var destinationElement) ? destinationElement.GetString() : null;
        var failureReason = payload.TryGetProperty("reason", out var reason) ? reason.GetString() : null;
        var failureCode = payload.TryGetProperty("code", out var code) ? code.ToString() : null;

        var candidates = new[] { gupshupId, messageId }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        WhatsAppMessageLog? log = null;
        foreach (var candidate in candidates)
        {
            log = await db.WhatsAppMessageLogs.FirstOrDefaultAsync(
                x => x.ExternalMessageId == candidate,
                cancellationToken);
            if (log is not null) break;
        }

        if (log is null)
        {
            logger.LogWarning(
                "Gupshup message event could not be matched to an outbound log. Status={Status}, MessageId={MessageId}, GsId={GsId}, Destination={Destination}",
                eventStatus, messageId, gupshupId, destination);
            return;
        }

        // Enqueued uses the Gupshup message ID; later DLR events use gsId.
        if (!string.IsNullOrWhiteSpace(gupshupId))
            log.ExternalMessageId = gupshupId;
        else if (!string.IsNullOrWhiteSpace(messageId))
            log.ExternalMessageId = messageId;

        log.DeliveryStatus = string.IsNullOrWhiteSpace(eventStatus) ? "Unknown" : eventStatus;
        if (!string.IsNullOrWhiteSpace(failureReason) || !string.IsNullOrWhiteSpace(failureCode))
            log.ErrorMessage = $"{failureCode}: {failureReason}".Trim(' ', ':');

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Gupshup message status updated. BusinessId={BusinessId}, Status={Status}, ExternalMessageId={ExternalMessageId}",
            log.BusinessId, log.DeliveryStatus, log.ExternalMessageId);
    }

    private async Task HandleGupshupInboundMessageAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("payload", out var payload))
            return;

        var source = payload.TryGetProperty("source", out var sourceElement) ? sourceElement.GetString() : null;
        var messageType = payload.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
        var messageId = payload.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
        var senderName = payload.TryGetProperty("sender", out var sender) &&
                         sender.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString()
            : null;

        var text = string.Empty;
        if (payload.TryGetProperty("payload", out var messagePayload))
        {
            if (messagePayload.ValueKind == JsonValueKind.Object &&
                messagePayload.TryGetProperty("text", out var textElement))
                text = textElement.GetString() ?? string.Empty;
            else if (messagePayload.ValueKind == JsonValueKind.String)
                text = messagePayload.GetString() ?? string.Empty;
        }

        logger.LogInformation(
            "Gupshup inbound message received. Source={Source}, Type={MessageType}, MessageId={MessageId}, Sender={SenderName}, TextLength={TextLength}",
            source, messageType, messageId, senderName, text.Length);

        // Gupshup's shared source number is not the same identifier as Meta's
        // per-business Phone Number ID. We therefore acknowledge and log the inbound
        // event here; the existing Meta flow remains unchanged. Business routing for
        // Gupshup can be added later when each business has its own Gupshup source.
    }


    private static bool CryptographicEquals(string left, string right)
    {
        var leftBytes = System.Text.Encoding.UTF8.GetBytes(left);
        var rightBytes = System.Text.Encoding.UTF8.GetBytes(right);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

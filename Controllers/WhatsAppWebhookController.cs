using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UplivaAI.Models;
using UplivaAI.Services;
using UplivaAI.Data;
using Microsoft.EntityFrameworkCore;

namespace UplivaAI.Controllers;

[ApiController]
[Route("webhooks/whatsapp")]
public class WhatsAppWebhookController(
    IOptions<WhatsAppSettings> options,
    UplivaDbContext db,
    IWhatsAppFlowService flowService,
    ILogger<WhatsAppWebhookController> logger) : ControllerBase
{
    private readonly WhatsAppSettings _settings = options.Value;

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
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        logger.LogInformation("WhatsApp webhook POST received. PayloadLength: {Length}", body.Length);

        try
        {
            var payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

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
}

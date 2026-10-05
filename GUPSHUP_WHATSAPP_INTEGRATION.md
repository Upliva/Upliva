# UplivaAI - Gupshup WhatsApp Integration

This implementation adds a server-side Gupshup template send flow while keeping the existing Meta Cloud API integration intact.

## 1. Architecture

Browser/UI
 -> UplivaAI MVC Controller
 -> IGupshupWhatsAppService
 -> Gupshup WhatsApp API
 -> Customer WhatsApp

Gupshup callbacks
 -> ngrok (development)
 -> /webhooks/whatsapp
 -> status update in WhatsAppMessageLogs

## 2. Gupshup configuration

Keep non-secret defaults in appsettings.json:

```json
"WhatsApp": {
  "Gupshup": {
    "BaseUrl": "https://api.gupshup.io",
    "AppName": "",
    "SourceNumber": "",
    "ApiKey": "",
    "WebhookSecret": ""
  }
}
```

For local development use User Secrets. Never commit the API key:

```powershell
dotnet user-secrets set "WhatsApp:Gupshup:ApiKey" "YOUR_NEW_GUPSHUP_API_KEY"
dotnet user-secrets set "WhatsApp:Gupshup:AppName" "YOUR_GUPSHUP_APP_NAME"
dotnet user-secrets set "WhatsApp:Gupshup:SourceNumber" "YOUR_SOURCE_WHATSAPP_NUMBER"
```

If a webhook secret is configured in UplivaAI, also configure Gupshup's custom webhook header `X-Upliva-Webhook-Secret` with the same value.

## 3. Configure the approved template

Open Admin -> WA templates -> Add template.

For the screenshot example:

- Name: `testing`
- Provider: `Gupshup`
- Gupshup Template ID: paste the approved Gupshup template ID, not only the display name
- Language: use the approved language for the template
- Category: use the approved category
- Body:

```text
Hi {{BusinessName}},

We're happy to inform you that your order {{ViewProduct}} has shipped! Click below to view the status of your shipment.
```

- Button text: `View order`

The send screen automatically detects `BusinessName` and `ViewProduct` and creates two value fields.

## 4. Send a message

Admin -> WA templates -> Send.

Select:
- Business
- Recipient WhatsApp number with country code, e.g. `919798555992`
- BusinessName value
- ViewProduct value

Click `Send WhatsApp template`.

The browser never calls Gupshup directly. UplivaAI sends the request from the server.

## 5. Gupshup request

UplivaAI posts to:

`https://api.gupshup.io/wa/api/v1/template/msg`

with:
- `channel=whatsapp`
- `source`
- `destination`
- `src.name`
- `template={"id":"...","params":[...]}`

The API key is sent in the `apikey` header.

## 6. Webhook

Development callback:

`https://CURRENT-NGROK-URL.ngrok-free.dev/webhooks/whatsapp`

The endpoint has `[AllowAnonymous]` because Gupshup is an external server. The rest of the UplivaAI application remains authenticated.

The controller supports:
- existing Meta Cloud API webhook JSON
- Gupshup v2 `message-event` callbacks
- Gupshup v2 inbound `message` callbacks

Gupshup delivery statuses such as `enqueued`, `failed`, `sent`, `delivered`, and `read` update the matching `WhatsAppMessageLog` when the returned message ID can be matched.

## 7. No EF migration for this change

This integration reuses the existing WhatsAppTemplateConfigurations and WhatsAppMessageLogs tables. The code changes do not add database columns or tables, so no new migration is required for this integration.

## 8. Security

- Do not put the Gupshup API key in source code.
- Rotate any API key previously exposed in chat/screenshots.
- Use User Secrets locally.
- Use Azure App Service configuration/Key Vault in production.
- For production webhooks, configure `WebhookSecret` and the matching Gupshup custom header.
- ngrok is for development/demo; use a stable HTTPS endpoint for production.

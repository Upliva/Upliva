# UplivaAI WhatsApp outbound testing — current secret format

This build keeps the **existing flat configuration keys** exactly as currently used. You do not need to change the structure of your existing User Secrets.

```json
{
  "WhatsApp:WebhookVerifyToken": "your-existing-value",
  "WhatsApp:PhoneNumberId": "your-existing-phone-number-id",
  "WhatsApp:AccessToken": "your-existing-meta-access-token",
  "Admin:Password": "your-existing-admin-password",
  "Admin:Name": "UplivaAI Administrator",
  "Admin:Email": "uplivasupport@gmail.com"
}
```

## For this outbound-only test

- `WhatsApp:PhoneNumberId` is required.
- `WhatsApp:AccessToken` is required.
- `WhatsApp:WebhookVerifyToken` is optional and is **not required** for sending the test catalog.
- `WhatsApp:GraphApiVersion` is optional; the application falls back to `v26.0`.
- The real access token is never displayed in the admin page.
- The code also supports the nested `WhatsApp` section from `appsettings.json`, but your current flat User Secrets keys remain the primary supported test format.

## Test flow

1. Start UplivaAI in Development.
2. Open Admin → Business → WhatsApp Business → Manage.
3. Enable WhatsApp integration and save.
4. Select/rank catalog products and save.
5. Click **Test WhatsApp connection**.
6. Enter your test recipient number.
7. Choose **Selected WhatsApp products**.
8. Click **Send catalog now**.

No webhook or public callback URL is needed for this outbound-only test.

# WhatsApp outbound-only testing

This version is configured so outbound Meta WhatsApp message testing does not depend on webhook verification.

## User Secrets
Use the existing project User Secrets and set:

```json
{
  "WhatsApp": {
    "GraphApiVersion": "v26.0",
    "PhoneNumberId": "YOUR_META_PHONE_NUMBER_ID",
    "AccessToken": "YOUR_CURRENT_META_ACCESS_TOKEN",
    "WebhookVerifyToken": "OPTIONAL_LATER"
  }
}
```

Do not commit the access token.

## Admin test flow
1. Run the application.
2. Open Admin > Business > WhatsApp Business > Manage.
3. Leave Access Token blank if it is in User Secrets.
4. Webhook Verify Token can remain blank for outbound testing.
5. Enable WhatsApp integration and save.
6. Select/rank WhatsApp catalog products and save.
7. Use **Send catalog to my WhatsApp**.
8. Enter the test recipient in digits with country code, e.g. `919798555992`.
9. For the Meta test number, first message `Hi` from the recipient phone so the test conversation is active.

The service resolves business credentials in this order: per-business DB value first, then `WhatsApp:*` configuration/User Secrets.

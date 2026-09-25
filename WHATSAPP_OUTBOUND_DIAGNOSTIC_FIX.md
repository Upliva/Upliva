# WhatsApp outbound diagnostic fix

This patch keeps the existing flat User Secrets format:

- `WhatsApp:PhoneNumberId`
- `WhatsApp:AccessToken`
- `WhatsApp:WebhookVerifyToken`

Changes:
1. Explicitly loads User Secrets in Program.cs.
2. Business page considers a DB token OR User Secret token configured.
3. Adds **Send one test text first**.
4. Meta API errors are shown with HTTP status, Meta error code/message/details.
5. Catalog sending uses the same detailed Meta response, so the first failing product is diagnosable.
6. Webhook verify token remains optional for outbound testing.

Recommended test sequence:
1. Restart Visual Studio after changing User Secrets.
2. Open Manage page and confirm the green `Meta outbound credentials detected` banner.
3. Save WhatsApp Business & catalog ranking with integration enabled.
4. In Meta API Testing, ensure `+91-97985-55992` is registered as the recipient.
5. From +91-97985-55992, send `Hi` to the Meta test number.
6. In UplivaAI enter `919798555992`.
7. Click **Send one test text first**.
8. If successful, click **Send catalog now**.

If Meta returns an error such as a 24-hour messaging-window error, use the exact Meta error shown by the toast; do not guess at the token.

# WhatsApp Catalog Outbound Testing

This patch adds a production-oriented admin test panel to the WhatsApp Business setup page.

## What it does

From `AdminBusinessIntegration/Manage/{businessId}` an admin can:

1. Enter the personal WhatsApp number that should receive the test.
2. Choose:
   - **Selected WhatsApp products** — only products checked/ranked in the WhatsApp catalog section.
   - **All active catalog products** — every active catalog item for the business.
3. Click **Send catalog now**.
4. UplivaAI uses the business-specific Meta Phone Number ID, Access Token and Graph API version stored in `BusinessWhatsAppSettings`.
5. It sends a catalog introduction followed by each product. If a product has an image URL, Meta receives an image message with the full catalog details as the caption. Products without images are sent as text.
6. Existing outbound message logging is used for each message.

## Testing steps

1. Start the UplivaAI application.
2. Open the approved business's **WhatsApp Business** management page.
3. Confirm:
   - Phone Number ID is the Meta test phone's Phone Number ID.
   - Access Token is current.
   - Graph API Version matches the version configured in Meta.
   - WhatsApp integration is enabled.
4. Click **Test WhatsApp connection** first.
5. From the personal recipient WhatsApp account, send `Hi` to the Meta test number. This is recommended before sending free-form text/image messages.
6. In UplivaAI, confirm the catalog products and their WhatsApp ranking are saved.
7. Enter the recipient number in international digits only, for example `919798555992`.
8. Choose **Selected WhatsApp products** for the first test.
9. Click **Send catalog now**.
10. Check the recipient WhatsApp chat.

## Important

- This is an outbound catalog showcase test, not a native Meta Commerce Catalog/product-card integration.
- Native WhatsApp Commerce Catalog/Product messages are a later integration step.
- No database migration is required by this patch. The added test fields exist only in the MVC view model and are not persisted.
- Access tokens must not be committed to source control. Production deployments should use a secret manager/encrypted storage.
- If Meta rejects a message, inspect the Visual Studio Output window for the HTTP status and Meta response.

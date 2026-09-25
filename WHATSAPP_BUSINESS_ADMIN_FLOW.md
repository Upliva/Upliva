# WhatsApp Business Admin Flow

1. Marketing page/chatbot captures a lead.
2. Admin logs in.
3. Admin marks the lead Interested and selects a plan.
4. The Interested lead appears on the first admin dashboard in its plan category.
5. Admin creates the Business. The business becomes Approved immediately.
6. UplivaAI creates an empty BusinessWhatsAppSettings record for that business.
7. Admin opens WhatsApp Business setup and stores WABA/Phone Number ID/token/webhook token.
8. Admin adds catalog items using the business-type dynamic template.
9. Admin selects and ranks the products for WhatsApp.
10. Admin previews the selected catalog in WhatsApp mode.
11. Meta sends inbound messages to `/webhooks/whatsapp`.
12. UplivaAI resolves the business using PhoneNumberId, logs the message, serves the menu/catalog and creates WhatsApp enquiries.

Website publishing, custom domains and website content are intentionally out of the active MVP scope.

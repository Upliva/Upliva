# WhatsApp-first refactor notes

## Kept working flows

- Marketing page
- Marketing chatbot
- Lead capture / ChatbotLeads
- Admin login / cookie authentication
- Interested / Not Interested lead workflow
- Plan selection and admin dashboard categories
- Interested lead -> Business creation

## WhatsApp-focused implementation

- Business is operationally approved immediately when created from an Interested lead.
- BusinessWhatsAppSettings is created with the business.
- Dynamic catalog templates are selected from BusinessType/CatalogTemplateKey.
- Common catalog fields stay strongly typed.
- Business-specific catalog data is stored in CustomAttributesJson.
- WhatsApp product selection is controlled by IsWhatsAppTopPick + SortOrder.
- WhatsApp preview reads live SQL data.
- WhatsApp webhook resolves the tenant by PhoneNumberId.
- Incoming/outgoing WhatsApp messages are logged.
- Plain-text customer messages create BusinessEnquiry records.
- Website/custom-domain functionality is excluded from the active build and reserved for a later module.

## Database change

The active EF model no longer includes website-specific tables/columns. Generate a new migration from the current model:

```powershell
Add-Migration WhatsAppFirstMvp -OutputDir Migrations
Update-Database
```

Back up an existing database before applying this migration because website-specific schema elements are intentionally removed from the active model.

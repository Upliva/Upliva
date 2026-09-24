# UplivaAI Registration - Lead Only Flow

## Public registration
The public form intentionally asks for only:

1. Name
2. Business category
3. WhatsApp / phone number

It creates a `ChatbotLeads` record only. It does not create a `Business`, `PlatformUser`, `WebsiteConfiguration`, catalog, WhatsApp settings, password, or account.

## Lead lifecycle
`New -> Contacted -> Interested -> Confirmed -> Converted`

Alternate outcome: `NotInterested`.

Admin can update the status from the Marketing & Sales Pipeline page. Timestamps are stored for Contacted, Confirmed, and Converted. `ConvertedBusinessId` is nullable and is intended to be populated when a future onboarding workflow creates the real Business account.

## After confirmation
Only after the business team confirms onboarding should the platform collect the detailed business profile, catalog images, brochure/PDF, website URL, logo, website content and WhatsApp configuration.

## Storage
Current local image/PDF storage remains unchanged. It can later be replaced behind the existing storage abstraction with Azure Blob Storage.

## Database
This change is designed for a clean database reset. Since the database was deleted, run:

```powershell
Add-Migration InitialCreate
Update-Database
```

Do not delete the database or migration folder after that unless a deliberate clean reset is required.

# UplivaAI Phase 1 – Security and Setup

## What this phase implements

- Business registration remains a lead until an Admin confirms it.
- Confirmed businesses get a Business Owner account with mobile-number login and a temporary password.
- Temporary passwords are hashed and the owner must change the password on first login.
- Business owners are isolated by the `BusinessId` claim; controllers must resolve ownership server-side.
- Public business pages support SQL-backed search, categories, offers, enquiry capture and tap-to-call tracking.
- Enquiries are saved before notification is queued.
- Email notifications use a SQL-backed retry queue; notification failure never removes an enquiry.
- Public enquiry and call-click endpoints are rate limited and use anti-forgery protection where state is changed.
- Enquiry form has a honeypot and server-side phone/message validation.
- Azure Blob Storage is behind `IBlobStorageService` and images are validated by extension, content type, size and file signature.
- Public `/media` access is restricted to approved business profile/banner/catalog blobs. Private documents are not served by this endpoint.
- Correlation IDs are accepted from `X-Correlation-ID` only when <=100 chars, returned in the response, and written to error/integration/audit records.
- Production operational logs are stored in SQL Server: `ErrorLog`, `IntegrationLog`, `NotificationLog`, `AuditLog`.
- Log retention deletes integration/notification logs older than 60 days and application errors older than 90 days.
- `/health` checks SQL and Azure Blob connectivity. Gupshup is reported as optional/not configured until Phase 4.
- No Gupshup API key or production WhatsApp credentials are included in source code.

## Secrets

Do not put these values in `appsettings.json`:

- Azure Storage connection string
- Admin password
- SMTP password
- Gupshup/Meta access token
- WhatsApp webhook verification secrets
- Visitor hash salt for production

Use Visual Studio Package Manager Console or a terminal:

```powershell
dotnet user-secrets set "AzureStorage:ConnectionString" "<secret>"
dotnet user-secrets set "AzureStorage:ContainerName" "upliva-media"
dotnet user-secrets set "Security:VisitorHashSalt" "<long-random-secret>"
dotnet user-secrets set "Admin:Email" "<admin-email>"
dotnet user-secrets set "Admin:Password" "<strong-admin-password>"
```

For email notifications:

```powershell
dotnet user-secrets set "Notifications:SmtpHost" "<smtp-host>"
dotnet user-secrets set "Notifications:SmtpPort" "587"
dotnet user-secrets set "Notifications:SmtpUsername" "<smtp-user>"
dotnet user-secrets set "Notifications:SmtpPassword" "<smtp-password>"
dotnet user-secrets set "Notifications:FromEmail" "<verified-from-email>"
dotnet user-secrets set "Notifications:FromName" "UplivaAI"
dotnet user-secrets set "Notifications:EnableSsl" "true"
```

Gupshup/Meta credentials remain disabled until the later integration phase.

## Database migration

This source tree intentionally does not contain a generated EF migration because the target database must be the database used by your Visual Studio environment.

After opening the solution in Visual Studio and confirming the `DefaultConnection` points to the intended database:

```powershell
Add-Migration Phase1ScalableArchitecture -OutputDir Migrations
Update-Database
```

Before `Update-Database`, review the generated migration. It should add at least:

- `CallEvents`
- `NotificationLogs`
- `IntegrationLogs`
- `PlatformUsers.MustChangePassword`
- `Business.LogoBlobName`
- `Business.BannerBlobName`
- `MarketingLead.RegistrationStatus`
- relevant indexes

Do not run the migration against a production database without a backup and review.

## Testing order

1. Start SQL Server.
2. Run the EF migration.
3. Configure Admin secrets.
4. Configure Azure Blob secrets.
5. Start UplivaAI.
6. Open `/health`.
7. Register a test business.
8. Admin confirms it and creates the owner account.
9. Log in using mobile number + temporary password.
10. Change the password.
11. Upload a logo/banner and catalog image.
12. Open `/business/<slug>`.
13. Test search/categories/offers.
14. Submit an enquiry and verify it exists in SQL before checking email.
15. Test call button and verify `CallEvents`.
16. Temporarily break SMTP configuration and confirm the enquiry remains stored and `NotificationLog` becomes retryable/failed.
17. Restore SMTP and verify retry succeeds.
18. Test an invalid/oversized/non-image upload.
19. Test that Business A cannot access Business B catalog, enquiries or profile.
20. Only after Phase 1 is stable, configure Gupshup.

## Production notes

- Use HTTPS only.
- Use a strong random visitor hash salt in production.
- Rotate any secret that has ever been committed to a repository or shared in chat.
- Keep Azure Blob containers private; public business media is streamed through the controlled `/media` endpoint.
- Keep customer PII out of application/integration logs unless it is necessary for troubleshooting.
- Do not expose stack traces to customers. The customer only receives a correlation/reference ID.

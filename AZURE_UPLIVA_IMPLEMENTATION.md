# Upliva Azure + Business Owner + Public Catalog Implementation

## Architecture

```text
Business registration/lead
        |
        v
Admin confirmation
        |
        +--> Business record
        +--> Business Owner account (mobile User ID + temporary password)
        |
        v
Business Owner Dashboard
        |
        +--> Business Profile
        +--> Catalog
        |      |
        |      +--> Azure Blob Storage (business/{BusinessId}/catalog/*)
        |      +--> SQL catalog metadata
        |
        +--> Leads / Enquiries
        |
        v
Public page: /business/{slug}
        |
        +--> Search/categories/images
        +--> Enquiry
        |
        v
BusinessEnquiry -> Business Owner Dashboard

Admin
  +--> WhatsApp template configuration
  +--> Gupshup integration later
```

## Loose coupling
- Controllers use `IBlobStorageService`, not Azure SDK types.
- Azure credentials are configuration-only and are never stored in source control.
- WhatsApp templates are stored as provider-neutral configuration with `Provider = Gupshup`; the sender can be implemented later behind a provider interface.
- Public catalog is independent from the WhatsApp sender.

## Azure setup
Store the connection string in User Secrets: `dotnet user-secrets set "AzureStorage:ConnectionString" "<connection-string>"`
The default container is `upliva-media`; the service creates it automatically. If you want to reuse the existing `furnitureshop` container for the first test, set `AzureStorage:ContainerName` to `furnitureshop`; for production, keep one dedicated Upliva container and use business-specific virtual folders. Images remain private in storage and are streamed through `/media/...`.

## EF migration
Run from Visual Studio Package Manager Console after reviewing the model changes:

```powershell
Add-Migration AddAzureMediaBusinessOwnerAndWhatsAppTemplates -OutputDir Migrations
Update-Database
```

## Gupshup
Do not put the real API key in `appsettings.json` or source control. Add it only when the Gupshup phase is enabled. The new admin template screen stores the approved template name, language, category, body and button URL contract; actual sending remains a separate provider service.

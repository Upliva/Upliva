# Upliva implementation change summary

Implemented as a loose-coupled first phase:

1. Azure Blob Storage abstraction (`IBlobStorageService`) with Azure implementation.
2. Catalog image upload from the Business Owner UI to Azure Blob Storage.
3. Private blob streaming through `/media/...` so the storage container does not need anonymous public write access.
4. Business Owner account creation after Admin converts/confirms an Interested lead.
5. Generated temporary password shown to Admin once; only the hash is stored.
6. Business Owner login supports the mobile number as the primary User ID.
7. Business Owner can change their password.
8. Business Owner can manage normal profile/catalog content; platform-level WhatsApp/plan/type settings remain Admin-controlled.
9. Public business catalog at `/business/{slug}` with WhatsApp-like presentation, search, categories and enquiry capture.
10. Admin WhatsApp template configuration UI, ready for the later Gupshup provider/send phase.
11. Existing Meta/Gupshup sending code was not replaced. Gupshup credentials/API calls remain a later phase.
12. Existing EF migrations were not replaced. Generate a new migration after reviewing the model changes.

## Required local setup

```powershell
dotnet user-secrets set "AzureStorage:ConnectionString" "<your Azure Storage connection string>"
dotnet user-secrets set "AzureStorage:ContainerName" "upliva-media"
```

If the storage account already has a test container named `furnitureshop`, it can be used temporarily by setting the container name accordingly.

## Database migration

```powershell
Add-Migration AddAzureMediaBusinessOwnerAndWhatsAppTemplates -OutputDir Migrations
Update-Database
```

## Important

- Do not commit the Azure Storage connection string.
- Do not commit Gupshup API keys.
- Gupshup Business Account/number/API integration is deliberately not enabled in this phase.
- Test the business conversion and catalog upload first, then connect Gupshup.

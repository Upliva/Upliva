# Local PDF Storage – Initial MVP

PDF brochures/catalogues are intentionally stored locally for the initial MVP.

## Storage location

`wwwroot/uploads/business/{BusinessId}/brochures/`

Each business has its own folder. Uploaded files are validated as PDF files and limited to 20 MB.

## Application boundary

The controllers use `IBusinessBrochureService` rather than writing directly to the file system.

Current implementation:

`IBusinessBrochureService -> BusinessBrochureService (local wwwroot storage)`

Future implementation:

`IBusinessBrochureService -> AzureBlobBusinessBrochureService`

The website, admin screens and business workflows do not need to change when storage moves to Azure Blob Storage.

## Important

Brochure files are not stored in SQL Server. The existing EF Core database/migrations are unchanged by this feature.

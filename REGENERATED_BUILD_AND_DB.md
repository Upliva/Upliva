# UplivaAI regenerated build/database steps

## 1. Build

Open this regenerated project in Visual Studio and run:

```powershell
dotnet restore
dotnet build
```

or use **Build > Build Solution**.

## 2. Fresh database with EF Core

The project intentionally contains no checked-in migration because the previous database was deleted and the current model is the fresh WhatsApp-first contract.

After the build succeeds, run in Package Manager Console:

```powershell
Add-Migration WhatsAppFirstMvp -OutputDir Migrations
Update-Database
```

Do not use `Add-Migration Initial`.

## 3. Fresh database with SQL Server script

Alternatively, create an empty SQL Server database and run:

`Database/WhatsAppMvpSchema.sql`

This script includes the current WhatsApp-first tables and `BusinessFollowUps`.

## 4. Catalog test path

Admin Login
→ Admin Dashboard
→ create/select Approved business
→ Catalog
→ Catalog entry mode
→ Manual entry OR Import CSV / JSON

For import:

Download CSV template
→ fill Product Name + any optional values
→ upload
→ Validate & Import
→ catalog list
→ WhatsApp Business
→ select/rank products
→ Preview WhatsApp

## 5. Required catalog data

Only `Product Name` is mandatory.

All common fields and all dynamic business-type fields are optional.

Dropdown/select fields remain available and their configured values are validated during CSV/JSON import.

## 6. Import formats

- CSV
- JSON array of catalog objects

The importer accepts common field names as well as dynamic field keys or display labels.

Imports are all-or-nothing: if one row is invalid, no row from that upload is inserted.

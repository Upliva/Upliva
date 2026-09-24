# Important – Existing EF Core Migrations

This package intentionally does NOT contain the `Migrations` folder.

The application/database in your current working copy already has a migration baseline that you successfully created and applied.

When updating your working project with this package:

1. Keep your existing `Migrations` folder exactly as it is.
2. Keep your existing database exactly as it is.
3. Do not run `Drop-Database`.
4. Do not run `Add-Migration` for these changes.
5. Do not replace `UplivaDbContextModelSnapshot.cs`.

These changes are application-level only:
- local PDF brochure storage behind `IBusinessBrochureService`
- safer PDF validation and file naming
- real-estate sample documentation/brochure
- existing customizable website sections

The EF schema is intentionally untouched.

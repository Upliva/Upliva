# UplivaAI - WhatsApp-first MVP

UplivaAI is currently focused on one operational product: **helping a business sell and communicate through WhatsApp**.

## Active flow

```text
Marketing page / chatbot
        ↓
ChatbotLeads
        ↓
Admin login → Interested + plan
        ↓
Create Business (active immediately)
        ↓
WhatsApp Business setup
        ↓
Dynamic Catalog Template
        ↓
Catalog items + images + business-specific fields
        ↓
Select + rank WhatsApp products
        ↓
WhatsApp preview
        ↓
Meta WhatsApp Cloud API
        ↓
Webhook → menu / catalog / enquiry
```

## Catalog architecture

The catalog is the core product data engine. Common data is stored in `BusinessCatalogItems`; business-specific fields are stored in `CustomAttributesJson`. Templates live under `CatalogTemplates/*.json`.

Examples:
- Real estate → BHK, area, location, property type, parking
- Furniture → material, colour, dimensions, warranty
- Restaurant → cuisine, food type, serving size, spice level
- Salon → service duration, treatment type, gender

The same data is used by the admin form, WhatsApp preview and WhatsApp message renderer.

## Database

SQL Server is the production source of truth. CSV/Excel should be treated as a future import/export mechanism, not the primary catalog store.

This refactor has no existing EF migrations in the source package. Create the first migration after opening the project:

```powershell
Add-Migration WhatsAppFirstMvp -OutputDir Migrations
Update-Database
```

A reference SQL Server schema is available at `Database/WhatsAppMvpSchema.sql`.

## Website

Website pages, custom domains and website-specific tables are deliberately deferred. The old website source is preserved as `FutureWebsite_Source_For_Later.zip` and is intentionally outside the active build. It can be restored later when website features are reintroduced.

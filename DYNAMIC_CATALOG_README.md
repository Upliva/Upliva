# Dynamic WhatsApp Catalog

This is the active catalog implementation for the WhatsApp-first MVP.

## Flow

```text
BusinessType
   ↓
CatalogTemplateKey
   ↓
CatalogTemplates/*.json
   ↓
Dynamic Add/Edit form
   ↓
BusinessCatalogItems
   └── CustomAttributesJson
   ↓
WhatsApp selection + ranking
   ↓
WhatsApp Preview / Meta WhatsApp messages
```

SQL Server is the source of truth. The JSON files define the shape of the dynamic fields; they are not the catalog data store.

Website visibility fields have been removed from the active catalog contract. Website rendering can be added later as a separate consumer of the same catalog data.

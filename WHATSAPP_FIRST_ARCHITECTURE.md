# UplivaAI WhatsApp-first architecture

## Current scope

The active product is a WhatsApp-first multi-business platform. The marketing page, chatbot lead capture and admin login are retained because they feed the WhatsApp business onboarding pipeline.

```text
Marketing Page / Chatbot
        |
        v
    ChatbotLeads
        |
        | Admin marks Interested + selects plan
        v
   Create Business
        |
        +--> Business
        +--> CatalogTemplateKey
        +--> BusinessWhatsAppSettings
        |
        v
  Dynamic Catalog
        |
        +--> common catalog fields
        +--> CustomAttributesJson
        +--> image URL
        |
        v
 WhatsApp selection/ranking
        |
        +--> IsWhatsAppTopPick
        +--> SortOrder
        |
        +------------+------------------+
        |                               |
        v                               v
WhatsApp Preview                 Meta WhatsApp Cloud API
                                        |
                                  Webhook / Messages
                                        |
                                  WhatsAppFlowService
                                        |
                         +--------------+---------------+
                         |                              |
                         v                              v
                  Catalog response                BusinessEnquiry
                         |
                         v
                  WhatsApp customer
```

## Catalog design

`BusinessCatalogItem` stores common fields in SQL Server. Business-specific fields are stored in `CustomAttributesJson` and are validated/rendered using a template under `CatalogTemplates`.

Examples:

- Real Estate: BHK, area, location, property type, parking
- Furniture: material, color, dimensions, warranty
- Restaurant: cuisine, food type, serving size, spice level
- Salon: service duration, gender, treatment type

The same renderer can produce the admin form, WhatsApp preview and WhatsApp message content.

## Storage rule

SQL Server is the source of truth. CSV/Excel can be added later as import/export, but CSV is not the production catalog database.

## Website

Website configuration, custom domains, public website rendering and website-specific tables are deliberately deferred. They can be added later without changing the core WhatsApp catalog contract.

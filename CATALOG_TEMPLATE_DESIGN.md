# Dynamic Catalog Design

## Why the catalog is central

The catalog is the product/service data source for the current WhatsApp MVP. Do not create a separate table or controller for every business type.

## Storage

### Common SQL columns

`BusinessCatalogItems` stores values that almost every business needs:

- Name
- Category
- SKU
- Brand / Model
- Price / original price / discount
- Image URL
- Short description / description
- Stock / availability
- Rating / reviews
- WhatsApp selected flag
- WhatsApp rank
- Active flag

### Business-specific data

`CustomAttributesJson` stores fields that vary by business type.

Example Real Estate:

```json
{
  "propertyType": "Apartment",
  "bhk": "3",
  "area": "1650 sq.ft",
  "location": "Lalpur, Ranchi",
  "bedrooms": "3",
  "bathrooms": "3",
  "parking": "2 cars"
}
```

Example Furniture:

```json
{
  "material": "Teak Wood",
  "color": "Brown",
  "dimensions": "7 x 3 x 3 ft",
  "warranty": "5 Years"
}
```

## Template

Each business type maps to a JSON template under `CatalogTemplates`.
The template defines:

- field key
- label
- type
- placeholder
- help text
- required flag
- WhatsApp visibility
- order
- select options

The controller validates the posted values against the template before writing JSON to SQL Server.

## Rendering

The same template is used by:

```text
Admin Add/Edit Catalog
        ↓
SQL Server
        ↓
WhatsApp Preview
        ↓
WhatsApp Flow
```

This avoids separate Real Estate/Furniture/Restaurant rendering code.

## CSV

CSV is an import/export format only. SQL Server remains the source of truth. The sample `SampleData/real-estate-catalog.csv` shows how a future bulk-import file can map common columns plus template-specific columns.

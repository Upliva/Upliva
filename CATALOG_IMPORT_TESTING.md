# Catalog import testing

## Admin path
1. Admin Dashboard
2. Open the created business
3. Catalog
4. **Import CSV / JSON**
5. Download CSV template
6. Fill Product Name plus any optional values
7. Upload
8. Validate & Import
9. Return to Catalog
10. Select products in WhatsApp Business and preview

## Supported files
- CSV
- JSON array of objects

Only Product Name is mandatory. Common fields and dynamic template fields are optional. CSV headers can use the field key or display label. For select fields, values must match one of the configured template options.

## All-or-nothing
If any row has a validation error, no rows are inserted. Correct the file and upload again.

## Example JSON
```json
[
  {
    "Name": "Premium 3 BHK Apartment",
    "Category": "Residential",
    "PriceText": "₹1.25 Cr",
    "propertyType": "Apartment",
    "bhk": "3",
    "area": "1650 sq.ft",
    "location": "Lalpur, Ranchi"
  }
]
```

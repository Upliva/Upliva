# UplivaAI Initial MVP Rules

## Business website

1. A business may already have its own website/domain.
2. UplivaAI must not force the business to use an UplivaAI website.
3. Admin can manually enter and verify the business public domain.
4. When a verified/enabled business domain exists, WhatsApp and public business links use that domain.
5. When no business domain is configured, the fallback is the UplivaAI `/business/{slug}` page.
6. Domain/DNS/SSL setup is intentionally manual in this first implementation.

## Catalog and WhatsApp

1. Business Owner can add, edit and delete catalog products.
2. The public business website shows all active catalog products.
3. Business Owner does not decide WhatsApp featured products.
4. Admin selects the WhatsApp featured products from the Business Integrations page.
5. Admin controls the maximum WhatsApp featured count, default 6, maximum 50.
6. WhatsApp sends only Admin-selected active products.
7. WhatsApp then provides the business public website link so customers can see the full catalog.

## Intentionally manual MVP operations

- Meta/WABA onboarding
- Phone Number ID mapping
- Access token rotation
- Webhook setup
- Domain verification/DNS
- WhatsApp featured product selection
- WhatsApp connection test

These are kept manual to make the first release easier to operate and troubleshoot. Automation can be added later without changing the core business model.

## Supported business categories

The initial MVP supports these selectable categories: Mobile Shop, Electronics Store, Retail, Grocery, Hardware, Furniture, Salon & Beauty, Restaurant, Pathology / Diagnostic, School / Education, Transportation, Real Estate, Banquet / Hotel / Resort, Rental Property, and Other. `BusinessType` remains a string field, so adding categories does not require an EF Core migration.

# Real Estate + PDF Catalog MVP Implementation

## What was added

- Real Estate sample data and a sample PDF brochure.
- PDF brochure upload/delete from Business → Website Content.
- PDF-only validation with a 20 MB limit and `%PDF-` signature check.
- Business-specific brochure storage under `wwwroot/uploads/business/{BusinessId}/brochures`.
- Public website automatically displays uploaded brochures.
- Existing WebsiteConfiguration flags are now editable for Catalog, Offers, Testimonials and WhatsApp visibility.
- Existing section text remains editable.
- Existing catalog `SortOrder` remains the simple way to control product order.
- No EF model/database schema changes were made.

## Section customization philosophy for the initial MVP

The initial release intentionally supports:

- edit section text/title
- show/hide major sections
- add/edit/delete products
- control product display order
- add/delete brochures
- admin-controlled WhatsApp featured products

A drag-and-drop page builder, arbitrary per-business section ordering and per-section left/right layout editor are deliberately not added to the first release. Those features would require additional persisted layout metadata and introduce more complexity. The current responsive layout is deterministic and safe. If a business needs a different arrangement, the section can be hidden and its information can be represented through the appropriate catalog/service/content section.

## Domain rule

- If no verified custom domain is configured, the UplivaAI `/business/{slug}` website is used.
- If a verified custom domain is configured, it is the canonical public URL and the fallback UplivaAI URL redirects to the business domain.
- Brochure assets remain platform-hosted in this MVP. For a business whose external website is hosted elsewhere, its external website remains the canonical customer destination. The brochure feature is intended for businesses using the UplivaAI-hosted page or a custom domain routed to UplivaAI.

## Migration

No migration is required for this release. Existing EF migrations remain unchanged.

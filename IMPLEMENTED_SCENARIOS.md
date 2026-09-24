# UplivaAI implementation scenarios

## Scenario 1 - Public marketing website
The existing UplivaAI marketing homepage is retained without changing its marketing UI.

## Scenario 2 - Business registration
Business owners register with business, owner, contact and password details. The registration creates a `Business` record with `Pending` status and a linked `BusinessOwner` account.

## Scenario 3 - Admin approval
Only an `Admin` can approve or reject a registered business.

## Scenario 4 - Admin publishing
A business must be approved before it can be published. Only an `Admin` can publish or unpublish it.

## Scenario 5 - Business owner access
The owner is linked to `BusinessId`. Login is blocked while the business is not approved. After approval, the owner can access business management features.

## Scenario 6 - Reusable business website
Approved and published businesses are served through `/business/{slug}` using the same application and database.

## Scenario 7 - Catalog and offers
Catalog items and offers are linked to `BusinessId`, allowing different businesses to manage their own content.

## Scenario 8 - Customer enquiries
Public business pages can create enquiries linked to the correct `BusinessId`.

## Scenario 9 - WhatsApp integration
Meta WhatsApp Cloud API is integrated through `/webhooks/whatsapp`. Incoming messages are processed by `WhatsAppWebhookController` and `WhatsAppFlowService`; outgoing messages are handled by `WhatsAppService`.

## Scenario 10 - Generic WhatsApp business flow
The WhatsApp flow is no longer tied to a resort. It can show a business menu, catalog, offers, website, location and contact information for the resolved business.

## Scenario 11 - Generic database architecture
`UplivaDbContext` is the EF Core context for platform data. It is not named after a particular business type and is reused for furniture, grocery, salon, restaurant, school and other businesses.

## Scenario 12 - Existing database reuse
The existing SQL Server database is reused. Database schema is managed exclusively through Entity Framework Core migrations; no custom schema initializer is used.

## Scenario 13 - Security
Cookie authentication and role authorization are used for `Admin` and `BusinessOwner`. Passwords are hashed using ASP.NET Core's password hasher. Sensitive configuration is kept outside source control.


## Domain and featured catalog scenarios
- Existing business domain configured: WhatsApp uses the business domain rather than the UplivaAI subdomain/path.
- UplivaAI URL remains available as a fallback and redirects to the business domain when the custom domain is verified and enabled.
- Business website lists all active products marked `ShowOnWebsite`.
- WhatsApp featured count is configurable per business; default 6, maximum 50.
- Admin controls the featured limit from Business Integrations.

## Initial multi-business website and WhatsApp operating rule

- Business owners manage their complete active product catalog.
- The public business website displays the complete active catalog.
- Platform Admin selects the smaller WhatsApp featured product subset from the Admin Business Integrations page.
- `FeaturedProductLimit` defaults to 6 and can be increased up to 50.
- WhatsApp sends only Admin-selected active products and then provides the business public website link for the rest of the catalog.
- If a business already has its own public domain/website, Admin can configure that domain as the public link; UplivaAI does not force its fallback URL.
- If no business domain is configured, UplivaAI uses `/business/{slug}` as the fallback public website.
- Initial domain and Meta/WhatsApp setup remains intentionally manual. No DNS automation or Meta onboarding automation is introduced in this MVP.

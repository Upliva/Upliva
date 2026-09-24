# UplivaAI - Initial Production MVP Validation

## Scope

This release intentionally keeps the first implementation small and manually operable.
It supports multiple businesses in one application/database and keeps all business data isolated by `BusinessId`.

## Public website rule

UplivaAI does **not** force every business to use an UplivaAI-hosted website.

1. If a business already has its own public website/domain, Admin can enter that domain and enable it.
2. WhatsApp website links then use the business's configured public domain.
3. If the business does not have its own website, UplivaAI uses `/business/{slug}` as the fallback public website.
4. A domain that is changed must be manually verified again before it can be enabled.

For an externally hosted website, UplivaAI only needs the public URL/domain for customer links. No DNS change to UplivaAI is required.

## Catalog and WhatsApp rule

- Business Owner can add, edit and delete catalog products.
- The public business website shows the full active catalog.
- Only Admin selects WhatsApp featured products.
- Admin sets the maximum WhatsApp featured-product count (1-50; default 6).
- WhatsApp sends only Admin-selected active products, up to the configured limit.
- Customers are given the business public website link to see the complete catalog.
- Business Owner cannot directly toggle WhatsApp featured status.

## Manual operations intentionally retained

The following are manual in the initial release:

- Meta/WABA onboarding
- Phone Number ID mapping
- Access-token configuration
- Webhook setup
- Domain/DNS verification
- WhatsApp featured-product selection
- WhatsApp connection testing

These can be automated later without changing the core business/catalog model.

## Safety corrections in this release

- Platform-level WhatsApp test endpoints require Admin authorization.
- An incoming webhook cannot send a response using platform-level credentials when its Phone Number ID does not map to a published, approved business.
- WhatsApp featured selection is Admin-only.
- Product selection IDs are validated against the target business and active catalog.
- Featured selection is limited to the configured maximum.
- Changing a configured custom domain clears its verification and disables it until manually verified again.
- Existing migration class/designer names are kept distinct.
- No new EF migration is required for these source changes.

## Database

Existing migrations remain:

- `20260923084450_InitialCreate`
- `20260924080000_AddAuditLogging`
- `20260924083000_OperationalHardening`
- `20260924090000_ConfigurableWhatsAppFeaturedLimit`

Run `Update-Database` after replacing the project. Do not create another migration just for this release.

## Runtime validation

The source was reviewed for the known migration/type-confusion errors and the affected files were corrected.
The build was not executed in this packaging environment because the .NET SDK is not installed here. The project should therefore still be rebuilt in Visual Studio before deployment.

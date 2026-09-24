# UplivaAI Operational Hardening

## Included

- Database-backed `ErrorLogs` with correlation ID, request path, user/business context and exception details.
- Admin error-log UI at `/AdminErrorLogs/Index`.
- Business edit save verification for both Admin and BusinessOwner, including linked owner account verification.
- Admin business integration page for published businesses at `/AdminBusinessIntegration/Manage?id={businessId}`.
- Per-business WhatsApp mapping using `BusinessWhatsAppSettings` and Phone Number ID.
- Per-business WhatsApp outgoing messages using the mapped Phone Number ID/token.
- WhatsApp connection test from the admin UI.
- Custom domain fields on `Businesses` and root-host routing for verified published domains.
- WhatsApp website links use the verified custom domain when enabled; otherwise they use `/business/{slug}`.
- Existing audit logging and caching retained.

## Database

Run the existing migrations plus:

`Update-Database`

The new migration is `20260924083000_OperationalHardening`.

## Admin workflow

1. Approve the business.
2. Publish the business.
3. Open **Integrations** from the Admin Dashboard.
4. Enter WABA ID, Phone Number ID, token and Graph API version.
5. Enable WhatsApp and save.
6. Use **Test WhatsApp connection**.
7. For a custom domain, configure DNS first, enter the domain, confirm DNS verification, and enable it.

Meta/WhatsApp still requires the normal business/phone onboarding and webhook subscription. The UplivaAI admin page manages the mapping and application configuration; it does not replace Meta's ownership/onboarding controls.

## Regression checks

- Registration/login
- Admin approve/reject/publish/unpublish
- Admin edit business and save
- Business owner edit business and save
- Website content save
- Catalog add/edit/delete/top-six
- Offers add/publish
- Public business page
- WhatsApp webhook mapping by Phone Number ID
- WhatsApp catalog links
- Audit logs
- Error logs
- Custom domain root page

## Environment limitation

This package was source-checked and structurally reviewed in the build environment, but the .NET SDK/SQL Server runtime was not available here, so an actual ASP.NET runtime build against the user's SQL Server could not be executed in this environment.


## Domain and catalog rules
- A verified custom domain is the preferred public/canonical business URL. The UplivaAI `/business/{slug}` URL is a fallback only.
- WhatsApp website links use the verified custom domain when enabled; otherwise they use the UplivaAI business URL.
- The business website shows the complete active catalog.
- WhatsApp featured products are configurable per business through Admin Business Integrations. The default is 6 and the supported range is 1-50.

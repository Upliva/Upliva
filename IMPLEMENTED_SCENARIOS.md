# UplivaAI implementation scenarios

## Scenario 1 - Public marketing website
Implemented at `/` with UplivaAI branding, WhatsApp-first positioning, lead-generation messaging, pricing, supported business types, workflow and registration CTA. No individual business is showcased on the public marketing homepage.

## Scenario 2 - Business registration
Implemented at `/BusinessRegistration/Register`.

The owner selects a business type, enters business/owner/contact details and creates a password. The registration is stored as `Pending`.

## Scenario 3 - Admin-only approval
Implemented with the `Admin` role and `/AdminDashboard`.

Only Admin can approve/reject a business.

## Scenario 4 - Admin-only publishing
A business must be approved before it can be published. Only Admin can publish/unpublish.

## Scenario 5 - Business owner access
The owner receives a `BusinessOwner` account during registration. Login is blocked until the business is approved. After approval, the owner can access the business dashboard.

## Scenario 6 - Business customization
Business owners can edit their own business profile and manage their own catalog/hot deals. Admin can edit any business.

## Scenario 7 - Reusable website
Every approved/published business can be served through `/business/{slug}`. The same application is reused; a separate application/hosting project is not required per business.

## Scenario 8 - Resort implementation preservation
The existing Paradise Palm booking implementation remains available internally. Existing booking, rooms, packages, WhatsApp test center and webhook routes were not removed. It is not showcased on the public UplivaAI marketing homepage.

- Resort home: `/Resort`
- Booking: `/Booking`
- Rooms: `/Rooms`
- Packages: `/Packages`
- WhatsApp test: `/WhatsApp`
- WhatsApp webhook: `/webhooks/whatsapp`

Reusable customer business pages use `/business/{slug}` only after the corresponding business is approved and published by an authorized admin.

## Scenario 9 - Hot deals and catalog
Added business-specific catalog and offer entities plus management screens. Business owners can add content; admin controls final publication of offers.

## Scenario 10 - Customer enquiries
Added a public enquiry form on each business website and stores enquiries against the business.

## Scenario 11 - Authorization foundation
Cookie authentication and role authorization are implemented with `Admin` and `BusinessOwner` claims. Passwords are hashed using ASP.NET Core's password hasher.

## Scenario 12 - WhatsApp platform roadmap
The existing working application-level WhatsApp integration remains untouched. A `BusinessWhatsAppSettings` model/table is included as the foundation for later mapping each customer's WhatsApp phone number/WABA to a business. Production tokens should be moved to a secure secret manager before enabling per-business secrets.

## Scenario 13 - Customer feedback
No fabricated feedback or public demo testimonials are shown. Business pages display only published, non-demo customer feedback added through the platform.

## Scenario 14 - Loading experience
The public UplivaAI marketing website includes a short branded loading screen before showing the main page.

## Backward-compatibility approach
The existing EF migration is preserved. New platform tables are created idempotently by `PlatformSchemaInitializer`, avoiding a rewrite of the committed resort migration. Existing resort database tables and WhatsApp services remain in place.

## Marketing v7 — Three-slide local-business pitch

- Public marketing homepage reduced to three focused scrolling sections: the WhatsApp lead hook, the UplivaAI offering, and local-business registration.
- Navigation reduced to Why UplivaAI, What you get, Get Started, Login and Register.
- Messaging is tailored toward Indian local businesses and Tier-2/Tier-3 contexts, including a Ranchi-first positioning without claiming unsupported customer counts.
- Added a 15-second local video advertisement at `wwwroot/videos/uplivaai-15s-ad.mp4`.
- The advertisement communicates: WhatsApp conversation -> website/offer -> enquiry/follow-up -> registration.
- No individual business demo is showcased on the public marketing homepage.
- Existing booking, WhatsApp, business registration, admin, business-owner and database functionality is preserved as the baseline.

## Marketing v8
- Three-section public marketing flow: Hook, What You Get + Feedback, Register.
- Friendly `/login` and `/register` routes.
- 15-second MP4 advertisement with voiceover and background audio.
- Removed location-specific Ranchi copy from advertisement.
- Added business-centric representative feedback cards, clearly marked illustrative.

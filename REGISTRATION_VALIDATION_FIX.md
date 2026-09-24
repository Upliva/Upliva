# Public Registration Validation Fix

The public `/register` page now uses a dedicated `LeadRegistrationViewModel`.

This is intentionally separate from `BusinessRegistrationViewModel`, which is retained for post-confirmation business provisioning and existing services.

## Public registration fields
- Name
- Business category
- WhatsApp / phone number

## Not collected at registration
- Password
- Confirm password
- Business name
- Email
- Address
- Website
- Catalog
- PDF

This prevents legacy password validation attributes from participating in the public lead form and keeps the registration flow lead-only.

The POST action still normalizes and validates the phone number, captures a `ChatbotLeads` record, and redirects to the existing success page. No Business or PlatformUser is created.

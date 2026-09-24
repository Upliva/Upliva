# Registration Fix

This version keeps the existing EF Core database/migration baseline untouched.

## Required at registration
- Business owner name
- WhatsApp number
- Password
- Confirm password

## Optional at registration
- Business name
- Business type
- Website title
- Email
- Phone number
- Address
- City
- State
- Postal code
- Country
- Business hours
- Description

Optional fields are nullable in the registration view model so ASP.NET Core's implicit required validation does not reject an otherwise valid minimal registration.

WhatsApp is normalized to digits and must contain 10-15 digits. It is stored as the business owner phone/login identifier.

If no email is supplied, the existing required/unique PlatformUser.Email database field receives an internal non-customer-facing value. Login accepts either the supplied email or the WhatsApp number.

No EF migration is required for this change.

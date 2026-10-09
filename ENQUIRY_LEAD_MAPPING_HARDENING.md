# Enquiry and lead-flow hardening

## Changes in this patch
- Removed the hidden `Website` honeypot from the public enquiry form. Browser autofill had the potential to populate the hidden field and cause valid customer enquiries to be silently discarded. Antiforgery validation and public-enquiry rate limiting remain enabled.
- Kept business lookup constrained to approved businesses by slug.
- Validated selected product IDs against the current business and active catalog before accepting an enquiry; invalid/cross-business product IDs are rejected with visible feedback rather than silently remapped to a general enquiry.
- Added a customer-facing success acknowledgement and visible validation-failure acknowledgement after redirect.
- Updated the business enquiries list to display the linked product name/category through a tenant-scoped left join. General enquiries remain supported.
- Added server-side status updates restricted to `New`, `Pending`, and `Completed`, with ownership/business scoping and antiforgery validation.
- No schema migration is required by these changes.

## Manual regression checklist
1. Open an approved business public catalog.
2. Submit a general enquiry and confirm the success acknowledgement appears.
3. Open a product card, choose Ask About This Product, submit, and confirm product ID maps to the exact product.
4. Confirm a product ID belonging to another business is rejected rather than attached to this business.
5. Check Business Enquiries: customer fields, source, product name/category, message and created time should be correct.
6. Change status to Pending and then Completed; reload and confirm the status persists.
7. Confirm Business Owner can only see/update enquiries belonging to their business.
8. Submit invalid phone/message/email values and confirm a visible error acknowledgement.
9. Confirm follow-up creation from an enquiry still carries the enquiry, product and customer details.
10. Verify rate limiting and antiforgery protection still apply.

## Build/test note
The current execution environment does not have the .NET SDK installed, so `dotnet build` and EF integration tests could not be run here. Run the project's normal build and test suite in Visual Studio/CI before deployment. No claim of a successful compile is made.

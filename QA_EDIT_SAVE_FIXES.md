# QA Edit/Save Fixes

## Fixed

1. Business profile Edit/Save
   - Added explicit anti-forgery token to the POST form.
   - Business owner and Admin can save the business profile.
   - Admin can open Edit Business for businesses regardless of publication state.
   - The correct dashboard is used as the Back link for Admin vs BusinessOwner.
   - When the business email/name/phone changes, the linked BusinessOwner platform user is synchronized so login remains consistent.
   - Duplicate platform-user email is rejected safely.
   - A toast reports exactly which business fields changed.

2. Website content Save
   - Added explicit anti-forgery token.
   - Toast reports the business name and exact content fields changed.
   - Cache invalidation and audit logging remain in place.

3. Catalog Add/Edit
   - Added explicit anti-forgery token to Add/Update form.
   - Existing EditCatalog/UpdateCatalog flow retained.
   - Toast reports the exact product that was added/updated.
   - Existing SKU and Top 6 rules retained.

4. Deals
   - Existing deal save/publication workflow retained.
   - Toast reports the exact deal title saved/published or the failure.

5. Admin actions
   - Explicit anti-forgery tokens added.
   - Approval/publish/unpublish/reject toasts identify the business name.

6. Toast UX
   - Added a reusable top-right toast in the Admin/Business workspace layout.
   - Success/info/error states are visually distinct.
   - Toast auto-dismisses after 5.5 seconds and can be closed manually.

## Existing functionality intentionally preserved

- Existing authentication and role authorization.
- Business registration and approval workflow.
- Existing Admin publish/unpublish rules.
- Existing catalog and WhatsApp Top 6 rules.
- Existing deal publication rule: BusinessOwner-created deals wait for Admin publication.
- Existing audit logging, correlation IDs, exception middleware and caching.
- No new database migration was added for these fixes.
- Existing EF migrations were not modified.


## Final edit/save hardening pass

- Business profile form posts explicitly to `BusinessManagement/Edit`.
- Phone and WhatsApp edit fields use length validation rather than restrictive `[Phone]` validation so common Indian formats such as `+91 97985 55992`, `919798555992`, spaces and hyphens are accepted.
- Per-field validation messages are displayed.
- Save button is disabled and changes to `Saving...` after submit to prevent accidental double submission.
- Business save is re-read from SQL Server after `SaveChangesAsync` before reporting success.
- Website content POST now loads the business before using `business.Name`, fixing the compile error and keeping the existing content-save flow.
- Website content form posts explicitly to `BusinessManagement/WebsiteContent`.
- Existing migrations, approval/publish flow, catalog flow, WhatsApp flow and offer publication flow are not changed by this pass.

# UplivaAI — Business + Catalog Edit/Save Fix

## Included

- New `BusinessManagementController` with secure business-scoped edit flow.
- New `BusinessEditViewModel`.
- New `Views/BusinessManagement/Edit.cshtml`.
- Admin Marketing leads now show **Edit business** after a lead has been converted.
- Admin Dashboard interested-lead table now shows **Edit business** for converted leads.
- Catalog page has a direct **Edit Business** action.
- Business edit re-reads the saved SQL row before reporting success.
- Business type changes automatically update `CatalogTemplateKey`.
- Catalog dynamic attributes now preserve stored keys that are not part of the current template while allowing every current template field to be changed or cleared.
- Existing catalog success/redirect behavior is retained.

## Business edit fields

Editable: Business name, business type, plan, owner, email, phone, WhatsApp number, address, city, state, postal code, country, hours, tagline, description and logo URL.

System-controlled/read-only: Id, slug, status, created/approved timestamps and template key (template key is recalculated from business type).

## Catalog edit behavior

- Product Name is the only required catalog field.
- Common fields are editable.
- Dynamic template fields are editable.
- Dropdown values are validated against the template.
- Dynamic fields can be cleared and saved.
- SKU uniqueness is checked within the business.
- Save is verified by reading the row back from SQL Server.
- Successful save redirects to the master catalog and shows the success message.
- Validation errors keep the edit form open.
- WhatsApp selection/ranking is not changed by a normal catalog edit.

## Manual smoke test

1. Open Marketing & Sales.
2. Find a converted lead such as HarHarMahadev.
3. Click **Edit business**.
4. Change Business Name, Owner, WhatsApp, Address, Description and Business Type one at a time.
5. Click **Save business changes**.
6. Confirm the success toast and reopen Edit Business. Confirm every changed value is still there.
7. Open Catalog.
8. Edit one product. Change Product Name, Category, Price, SKU, Image URL, description and every visible dynamic field.
9. Save. Confirm the edit form closes and the master catalog shows the new values.
10. Reopen the same product and verify persistence.
11. Clear an optional dynamic field and save; verify it remains blank after reopening.
12. Enter a duplicate SKU from another product; save must fail and the edit form must remain open.
13. Clear Product Name; save must fail because Product Name is the only required field.
14. Open WhatsApp Business and verify the selected/ranked products are unchanged by ordinary catalog edits.

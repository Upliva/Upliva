# Business deletion and stale business selection fix

## Bug
Deleted businesses could remain visible in the Admin Dashboard business selector because the application had no supported business deletion workflow and the selected ID was not validated against the current active-business list.

## Fix
- Admin Dashboard reloads the business list directly from SQL Server.
- GET response is marked `NoStore` so the browser does not reuse a cached dashboard.
- A selected business ID is accepted only when it exists in the current Approved business list.
- Admin can delete the selected business through a confirmation-protected POST action.
- Deletion clears `ChatbotLeads.ConvertedBusinessId` and deactivates business-owner accounts.
- Follow-ups are deleted first because their FKs use NO ACTION.
- Business child records are removed safely before the business.
- Audit history is retained and the business reference is cleared by the existing SET NULL FK.
- Business cache is invalidated after deletion.

## Expected result
After deleting a business and returning to Admin Dashboard, that business is absent from the selector immediately. A browser refresh also reads the current SQL Server state.

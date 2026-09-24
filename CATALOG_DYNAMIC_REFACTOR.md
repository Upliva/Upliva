# Dynamic Catalog Refactor

## What changed
- Catalog page is now a full-width master catalog list with image, item, category, price, availability, WhatsApp selection and actions.
- Search and dynamic category filtering are supported without changing the database schema.
- Add/Edit catalog form is below the list so existing items remain visible while adding data.
- Image URL is supported for each catalog item. The URL can point to an internet image today and can later be replaced by Azure Blob Storage URLs.
- Categories are data-driven from existing `BusinessCatalogItem.Category` values. New categories can be entered through the category datalist.
- WhatsApp Business page groups catalog items by category, supports search/category filtering, image preview, selection and ranking.
- Admin catalog access now works for approved businesses even if the original lead is no longer available.
- Existing `IsWhatsAppTopPick` and `SortOrder` fields remain the source of WhatsApp selection/ranking.

## Database / migrations
No database schema changes were made. Do not create a new migration for this refactor. Existing migrations can continue to be used to recreate a deleted development database.

## Main changed files
- `Controllers/BusinessContentController.cs`
- `Controllers/AdminBusinessIntegrationController.cs`
- `Models/WhatsAppFeaturedProductViewModel.cs`
- `Views/BusinessContent/Index.cshtml`
- `Views/AdminBusinessIntegration/Manage.cshtml`

## Flow
1. Open an approved business.
2. Manage catalog.
3. Add catalog item with name, category, price, description and image URL.
4. Saved item appears immediately in the master catalog list.
5. Open WhatsApp Business.
6. Select products by category.
7. Set ranking.
8. Save. WhatsApp showcase uses the normalized 1..N rank order.

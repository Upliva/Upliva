# UplivaAI Production QA Checklist

## Scope
This checklist covers registration/chatbot preservation, business lifecycle, dynamic catalog, CSV/JSON import, edit/update, WhatsApp selection/ranking, preview, enquiries/follow-up, business isolation, deletion, and marketing.

## P0 - Catalog core
- [ ] Create catalog with Product Name only.
- [ ] Product Name blank is rejected.
- [ ] Add every common optional field independently.
- [ ] Save without image.
- [ ] Save without price.
- [ ] Save without category.
- [ ] Save with all dynamic fields blank.
- [ ] Save select/dropdown field with a valid option.
- [ ] Reject invalid select value on server.
- [ ] Edit Product Name only and verify after reload.
- [ ] Edit Price only and verify after reload.
- [ ] Edit Category only and verify after reload.
- [ ] Edit Image URL only and verify after reload.
- [ ] Edit Description only and verify after reload.
- [ ] Edit a dynamic text field only and verify after reload.
- [ ] Edit a dynamic number field only and verify after reload.
- [ ] Edit a dynamic dropdown field only and verify after reload.
- [ ] Clear an optional dynamic field and verify it is removed from JSON.
- [ ] Update success message appears.
- [ ] Edit screen is closed after successful update.
- [ ] Failed validation keeps edit screen open.
- [ ] Delete item and verify it disappears after refresh.
- [ ] Cancel delete leaves item unchanged.

## P0 - Import
- [ ] Valid single-row CSV.
- [ ] Valid multi-row CSV.
- [ ] Valid JSON.
- [ ] Missing ProductName rejected.
- [ ] Invalid dropdown value rejected.
- [ ] Duplicate SKU in file rejected.
- [ ] Existing SKU rejected.
- [ ] Empty CSV rejected.
- [ ] Invalid JSON rejected.
- [ ] Unsupported extension rejected.
- [ ] Special characters and INR symbol preserved.
- [ ] Optional fields can be blank.
- [ ] Imported dynamic fields are visible on edit.
- [ ] Imported item can be edited and saved.

## P0 - WhatsApp ranking
- [ ] Select one product.
- [ ] Select multiple products.
- [ ] Deselect a product.
- [ ] Rank 1..N.
- [ ] Refresh and verify ranks persist.
- [ ] Duplicate submitted ranks are normalized deterministically.
- [ ] Rank below 1 is rejected server-side.
- [ ] Rank above configured limit is rejected server-side.
- [ ] More than configured FeaturedProductLimit is rejected.
- [ ] Product from another business cannot be selected by POST manipulation.
- [ ] Inactive product cannot be selected.
- [ ] Catalog screen displays selected WhatsApp rank.

## P0 - Preview
- [ ] Preview with zero selected products.
- [ ] Preview with one selected product.
- [ ] Preview with all selected products together.
- [ ] Product image/name/category/price render correctly.
- [ ] Dynamic fields render correctly.
- [ ] Rank order matches WhatsApp configuration.
- [ ] Preview reads current SQL data after edit.

## P0 - Business isolation/deletion
- [ ] Business A cannot see Business B catalog.
- [ ] Business A cannot update Business B catalog by changing posted IDs.
- [ ] Deleted business disappears from admin business selector after redirect/refresh.
- [ ] Deleted business cannot be selected by a stale URL.
- [ ] Related catalog/enquiry/follow-up/WhatsApp data is handled by deletion rules.
- [ ] Converted lead is not unintentionally deleted.

## P1 - Lead/chatbot
- [ ] Registration works.
- [ ] Marketing chatbot captures lead.
- [ ] Interested lead appears in admin.
- [ ] Business creation works.
- [ ] New business is Approved according to MVP rule.

## P1 - Enquiry/follow-up
- [ ] Enquiry can be created for a catalog item.
- [ ] Enquiry is isolated by business.
- [ ] Follow-up can be created.
- [ ] Follow-up status can change.
- [ ] Completed follow-up leaves active queue.

## P1 - Marketing
- [ ] Exactly three main marketing sections/slides.
- [ ] Video file loads and plays.
- [ ] Video fallback poster loads.
- [ ] Original UplivaAI scenario visuals load.
- [ ] CTA opens registration/chatbot flow.
- [ ] Mobile layout is readable.

## Database safety
- [ ] Confirm the active database name before testing.
- [ ] For a new empty database, apply the current EF migration/schema.
- [ ] Never create duplicate Initial migrations just because the database name changes.
- [ ] After migration, verify PlatformUsers, Businesses, BusinessCatalogItems, BusinessEnquiries, WhatsAppMessageLogs and BusinessFollowUps exist.

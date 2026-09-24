# QA Issue Fixes

Source baseline: existing `Dev_Upliva` project supplied for QA.

## Fixed issues

- #6 Adding a deal does not save data: the existing AddOffer persistence flow is retained, with an explicit post-save existence check and user-visible success/error feedback. The existing rule that BusinessOwner deals wait for admin publication is unchanged.
- #5 Product catalog entries can now be edited. Added EditCatalog/UpdateCatalog with the same ownership checks, validation, Top 6 limit, SKU duplicate protection, and website visibility behavior used by the existing add flow.
- #4 Pricing frequency is now displayed as `/ month` on all three marketing pricing cards.
- #3 UplivaAI logo wordmark now renders `Upliva` explicitly so the `A` cannot disappear because of an ambiguous path glyph.

## No database migration

No new database fields/tables were added. Existing migrations and schema remain unchanged.

## Files changed

- Controllers/BusinessContentController.cs
- Models/BusinessCatalogItemViewModel.cs
- Views/BusinessContent/Index.cshtml
- Views/Home/Marketing.cshtml
- wwwroot/images/upliva/upliva-logo.svg
- QA_ISSUE_FIXES.md

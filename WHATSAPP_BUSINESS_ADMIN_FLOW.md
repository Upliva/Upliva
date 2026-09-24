# UplivaAI WhatsApp Business Admin Flow

## Lead -> Approval -> WhatsApp Business -> Catalog

1. Marketing lead is captured.
2. Admin sets the lead to **Interested** and selects one of the three plans:
   - WhatsApp only
   - WhatsApp + Website
   - WhatsApp + Website + Business Enquiry
3. Admin creates the real Business record from the interested lead.
4. The Business record starts as **Pending**.
5. Pending businesses stay in the approval queue and are **not** included in the operational approved-business dashboard list.
6. Admin approves the business.
7. Only after approval does the business appear in **Approved WhatsApp businesses**.
8. Admin clicks **WhatsApp Business**. UplivaAI creates the per-business WhatsApp settings record if it does not already exist.
9. Admin enters the Meta WABA / Phone Number ID / token information and enables the WhatsApp connection.
10. Admin clicks **Add catalog** and adds products.
11. Back in **WhatsApp Business**, Admin selects the products that should appear on WhatsApp and assigns a rank.
12. Rank 1 is displayed first, rank 2 second, etc. The server normalizes the selected ranks to a clean 1..N order.
13. `WhatsAppFlowService` reads only `IsWhatsAppTopPick == true` products and orders them by `SortOrder`, so the saved ranking is the WhatsApp display order.

## Dashboard rules

- Pending / rejected businesses do not appear in the approved operational business list.
- A Business Owner dashboard is blocked until the business status is `Approved`.
- The admin approved-business actions are intentionally limited to:
  - WhatsApp Business
  - Catalog
- Website, Publish, Edit Business, Website Content and public-domain controls are not part of this WhatsApp onboarding screen.

## Plan behavior

### WhatsApp only
- WhatsApp catalog is the primary customer experience.
- The WhatsApp welcome menu does not show a Website option.
- If no catalog products are selected, WhatsApp reports that no products have been selected yet.

### WhatsApp + Website
- WhatsApp can show the selected ranked products.
- Website option can be shown in the WhatsApp menu.

### WhatsApp + Website + Business Enquiry
- Same WhatsApp + Website behavior, with business-enquiry functionality available elsewhere in the platform.

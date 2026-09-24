# Lead → Plan → Business → WhatsApp Catalog Flow

## 1. Lead qualification
A public chatbot creates a `ChatbotLeads` record. The Upliva team calls the prospect and sets the lead status to `Interested` or `Not Interested`.

## 2. Plan-first admin dashboard
When a lead is saved as `Interested`, it appears immediately on the first admin dashboard. There is no separate Business approvals screen.

The lead is grouped into one of these plan categories after the team selects the plan:

1. WhatsApp
2. WhatsApp + Website
3. WhatsApp + Website + Business Enquiry

If the plan is not selected yet, the lead remains in the `Interested — plan not selected` area.

## 3. Create the real business
From the selected plan category, the admin can create the real Business record. The business is created with `Status = Approved` and `ApprovedAtUtc` set immediately because the lead qualification is the approval gate in this workflow. There is no second business-approval step.

The lead remains `Interested` and is linked using `ConvertedBusinessId`.

## 4. WhatsApp Business
After the Business record exists, the admin can open **WhatsApp Business** and configure the Meta/WABA connection. The WhatsApp setup can then store the WABA ID, Phone Number ID, access token, webhook token, Graph API version, enabled state and featured-product limit.

## 5. Catalog ranking
Catalog products are selected for WhatsApp and assigned a rank. The submitted ranking is normalized to `1..N` and stored using the existing catalog fields (`IsWhatsAppTopPick` and `SortOrder`). WhatsApp showcase generation uses this order.

## 6. Database impact
No new database column is required for the plan-first workflow. Existing fields already provide the required state:

- `ChatbotLeads.Status`
- `ChatbotLeads.SelectedPlan`
- `ChatbotLeads.ConvertedBusinessId`
- `Businesses.ServicePlan`
- `Businesses.Status`
- `Businesses.ApprovedAtUtc`
- `BusinessCatalogItems.IsWhatsAppTopPick`
- `BusinessCatalogItems.SortOrder`

Existing public business registration can continue to use `Pending`; only the qualified lead-to-business path is changed to become active immediately.

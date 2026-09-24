# UplivaAI Lead Pipeline v2

## Public capture
The public registration page and marketing chatbot collect only:
- Name
- Business category
- WhatsApp / phone

They create a `ChatbotLeads` record only. No Business, PlatformUser, password, catalog, website configuration or WhatsApp business account is created at this stage.

## Phone-first qualification
Initial phase is manual: Upliva calls each lead. Admin updates the lead to Contacted, Interested or Confirmed. The plan is selected only after the customer confirms.

## Plans
1. WhatsApp only
2. WhatsApp + Website
3. WhatsApp + Website + Business Enquiry

## Not interested
Admin can permanently delete the lead. This is intentional for the initial phase.

## Later conversion
When the customer is confirmed and the team is ready to onboard them, the existing Business/PlatformUser/catalog/website/WhatsApp provisioning flow can be used. `ConvertedBusinessId` can link the lead to the created Business.

## WhatsApp catalog
Keep the existing `IsWhatsAppTopPick` + `FeaturedProductLimit` approach. The website can show the full active catalog; WhatsApp should show only the selected hot deals and link to the full website catalog.

## Storage
Current local image/PDF storage remains unchanged. It can later be replaced behind the existing storage abstraction with Azure Blob Storage.

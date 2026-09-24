# UplivaAI Marketing Chatbot & Visit Tracking

## Initial MVP flow
1. Public marketing site shows a small UplivaAI chatbot.
2. It asks exactly three questions: Name, Business Type, WhatsApp Number.
3. The lead is stored in `ChatbotLeads`.
4. The public marketing page creates an anonymous random visitor cookie and records page visits in `PlatformVisits`. No IP address is stored.
5. Admin → Leads & visits shows total visits, unique visitors, total chatbot leads, leads today and recent leads.
6. Admin can export up to 5,000 recent leads to CSV.

## Database migration
This feature adds two new tables, so unlike the earlier business-type-only change it **does require one EF Core migration**. Do not delete the existing database or migrations. From Visual Studio Package Manager Console with Default project `UplivaAI`, run:

```powershell
Add-Migration AddMarketingLeadAndVisitTracking
Update-Database
```

If `Add-Migration` reports an unexpected model difference, stop and inspect the generated migration before applying it.

## Marketing message
The confirmation message is intentionally short: `Your information was received successfully. Our team will reach out to you soon.` The opening voice positions the Upliva Platform as a friendly local business-growth platform and starts with `Johar Jharkhand!`, without promising a guaranteed business result.

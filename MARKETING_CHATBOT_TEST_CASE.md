# Marketing Chatbot Test Case

## Public flow
1. Open the UplivaAI marketing home page.
2. Click `Let's talk`.
3. Answer exactly three questions:
   - Please give your name.
   - What type of business do you have?
   - WhatsApp number.
4. Submit.
5. Expected confirmation: `Thank you. Our team will reach out to you soon.`

## Database
A row is created in `MarketingLeads` with:
- Name
- BusinessType
- WhatsAppNumber
- Source = MarketingChatbot
- VisitorId
- CreatedAtUtc

A page visit is created in `PlatformVisits` for the marketing home page and registration page. Unique visitors are calculated from the random `UplivaAI.VisitorId` cookie.

## Admin
Admin -> Leads & visits should show:
- Total visits
- Unique visitors
- Chatbot leads
- Leads today
- Recent leads
- Export CSV

## Invalid cases
- Empty name -> browser validation blocks submit.
- Empty business type -> browser validation blocks submit.
- Invalid WhatsApp number -> server returns an error.
- Duplicate submissions are not silently merged; each completed chatbot submission is a lead event.

## Migration
This feature introduces two new tables, so create exactly one new EF migration against the user's current migration history:

```powershell
Add-Migration AddMarketingLeadAndVisitTracking
Update-Database
```

Do not delete the database or the existing migrations.

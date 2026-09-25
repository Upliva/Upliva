# UplivaAI Optional-Field + Production Refactor

## Input rule
- Public chatbot/registration fields are optional.
- Admin business creation fields are optional.
- Business edit fields are optional.
- Catalog fields are optional except **Product Name**.
- Dynamic catalog template fields are optional.
- If WhatsApp is supplied, it is normalized to 91xxxxxxxxxx and duplicate business/active-lead numbers are blocked.
- Blank WhatsApp is allowed.

## Safe defaults
- Business name: lead name or `New Upliva Business`
- Business type: `Other`
- Plan: `WhatsApp + SMS`
- Country: `India`
- Catalog template: based on business type, falling back to generic

## Marketing page
Reduced to three concise production-use-case sections:
1. WhatsApp-first value proposition
2. Real Estate / Hotel / Retail / Services use cases
3. Two operational modes + registration CTA

## QA covered by unit tests
- Optional business fields
- Optional registration fields
- Optional WhatsApp normalization
- Duplicate WhatsApp lead protection
- Duplicate WhatsApp business protection
- Business defaults
- Catalog Product Name required only
- Dynamic catalog fields optional

## Database hardening
The DbContext now adds a filtered unique SQL Server index for active Interested leads with a WhatsApp number. This protects against concurrent duplicate registrations in addition to the application-level check.

For an existing database, create/apply the EF migration once:

```powershell
Add-Migration OptionalOnboardingAndLeadPhoneProtection
Update-Database
```

Do not create a new migration for every application restart.

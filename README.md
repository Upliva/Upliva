# UplivaAI

UplivaAI is a reusable ASP.NET Core MVC platform for registering and managing different types of businesses, publishing business websites, managing catalog/offers/enquiries, and integrating WhatsApp communication.

## Technical stack

- ASP.NET Core MVC / .NET 10
- C#
- Entity Framework Core 10
- SQL Server
- Meta WhatsApp Cloud API
- ASP.NET Core Cookie Authentication
- ASP.NET Core User Secrets for local secrets

## Architecture

```text
Marketing / Registration / Login
            |
            v
      ASP.NET Core MVC
            |
      Controllers
            |
        Services
            |
      UplivaDbContext
            |
      Existing SQL Server
            |
   +--------+---------+
   |                  |
Business data      WhatsApp data
   |                  |
   +--------+---------+
            |
            v
     Meta WhatsApp API
```

## Generic database context

`UplivaDbContext` is the application-level EF Core context. It is intentionally not named after one business type.

It contains platform entities such as:

- `PlatformUser`
- `Business`
- `BusinessCatalogItem`
- `BusinessOffer`
- `BusinessTestimonial`
- `WebsiteConfiguration` (browser title, SEO text, hero/about/services/CTA content and section visibility)
- `BusinessEnquiry`
- `BusinessWhatsAppSettings`

A business such as furniture, grocery, salon, restaurant or school is represented by a `Business` record and related records using `BusinessId`.

There is no separate DbContext or database for each customer business.

## Business registration flow

```text
Registration View
      |
      v
BusinessRegistrationController
      |
      v
IBusinessService / BusinessService
      |
      v
UplivaDbContext
      |
      v
Businesses table
      |
      +--> WebsiteConfigurations
      +--> BusinessCatalogItems
      +--> BusinessOffers
      +--> BusinessEnquiries
      +--> BusinessWhatsAppSettings
```

## Business lifecycle

```text
Register
   |
   v
Pending
   |
   v
Admin Review
   |
   +--> Reject
   |
   v
Approved
   |
   v
Published
   |
   v
/business/{slug}
```

## WhatsApp flow

```text
Customer WhatsApp
       |
       v
Meta WhatsApp Cloud API
       |
       v
/webhooks/whatsapp
       |
       v
WhatsAppWebhookController
       |
       v
WhatsAppFlowService
       |
       +--> UplivaDbContext
       |       |
       |       +--> Business
       |       +--> Catalog
       |       +--> Offers
       |       +--> Contact / Location
       |
       v
WhatsAppService
       |
       v
Meta Graph API
```

The webhook uses the incoming WhatsApp `phone_number_id` to resolve the configured business through `BusinessWhatsAppSettings` when that mapping exists.

## Database

The existing SQL Server connection is reused. The application does not create a database for every business. Database schema is managed exclusively with Entity Framework Core migrations.

For a fresh database, create the migration and database from the current models with `Add-Migration InitialCreate` and `Update-Database`. No custom schema initializer is required. The business model includes address, city, state, postal code, country and business hours so the generated site has complete contact information.

## Authentication

The existing marketing website and login modal are retained.

Roles:

- `Admin`
- `BusinessOwner`

Business owners are linked to their `BusinessId`. Admin users can review, approve, reject, publish and manage businesses.

## Local secrets

Never commit passwords or Meta access tokens.

Example User Secrets:

```powershell
dotnet user-secrets set "Admin:Name" "UplivaAI Administrator"
dotnet user-secrets set "Admin:Email" "your-admin-email@example.com"
dotnet user-secrets set "Admin:Password" "YOUR_STRONG_PASSWORD"
dotnet user-secrets set "WhatsApp:AccessToken" "YOUR_META_ACCESS_TOKEN"
dotnet user-secrets set "WhatsApp:PhoneNumberId" "YOUR_PHONE_NUMBER_ID"
dotnet user-secrets set "WhatsApp:WebhookVerifyToken" "YOUR_WEBHOOK_VERIFY_TOKEN"
```

## Run

```powershell
dotnet restore
dotnet build
dotnet run
```

For local Meta webhook testing, expose the HTTPS application with ngrok and configure:

```text
https://YOUR-NGROK-HOST/webhooks/whatsapp
```

## Public business website

Each approved/published business gets its own generated website route: `/business/{slug}`. The business page has its own website layout and does not use the UplivaAI marketing navigation. The browser title can be set to the business website title, the page uses the business name/content, and the footer identifies UplivaAI as the platform provider.

Because the entire application is hosted together, a bare URL such as `/SmartZoneMobiles` would conflict with application routes such as `/login`, `/register` and `/privacy`. The current safe route is `/business/smartzone-mobiles`. A future custom domain or subdomain can point to the same application without changing the business content model.

## Website content vs catalog

Catalog items are product/service records and can be shown on the public website independently of WhatsApp. `ShowOnWebsite` controls website visibility, while `IsWhatsAppTopPick` controls the WhatsApp Top 6 showcase and also highlights those items on the website. Business storytelling is kept separate in `WebsiteConfiguration`: hero text, About, Why Choose Us, Services, call-to-action, contact introduction, SEO description and footer text. This prevents long website copy from being forced into product records.

## Important design rule

Do not create `FurnitureDbContext`, `GroceryDbContext`, `SalonDbContext`, etc. for each business type.

Use the generic platform context:

```text
UplivaDbContext
      |
      +-- Business: Furniture
      +-- Business: Grocery
      +-- Business: Salon
      +-- Business: Restaurant
      +-- Business: School
```

If a future business type requires specialized domain tables, those entities can still be added as a separate module while keeping the platform context generic unless a genuine bounded context requires a separate database/context.


## Fresh database setup

This version uses Entity Framework Core migrations only. No custom schema initializer is used.

In Visual Studio Package Manager Console, with `UplivaAI` selected as the Default project:

```powershell
Add-Migration InitialCreate
Update-Database
```

If the database was deleted, run the same two commands to recreate it from the current model.
## Fresh database / EF Core migrations

This project uses Entity Framework Core migrations for database creation. There is no custom schema initializer.

If the database has been deleted or this is a fresh checkout, use Visual Studio Package Manager Console with **UplivaAI** selected as the Default project:

```powershell
Add-Migration InitialCreate
Update-Database
```

Then run the application. Do not manually create the database or tables in SQL Server.

The catalog `Rating` property is configured with SQL precision `decimal(3,2)`.


## Workspace navigation refinement (v10)
- Business-owner dashboard actions now use high-contrast buttons on the light workspace surface.
- All business management screens expose a visible Back to dashboard action.
- View website opens in the same tab so browser Back remains available.
- When an authenticated business owner/admin previews a public business site, a small Back to workspace bar is shown. Public visitors do not see this bar.
- Admin/business workspace header contains a persistent Dashboard link and Logout.
- No database model or migration changes were introduced by this UI refinement.

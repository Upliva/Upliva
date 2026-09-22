# UplivaAI - Reusable WhatsApp + Business Website Platform

This project started as the Paradise Palm Resort booking demo and now contains the reusable UplivaAI platform foundation while preserving the existing resort booking and WhatsApp implementation.

## Existing functionality preserved

- Paradise Palm rooms, packages and booking flow
- SQL Server / EF Core database
- WhatsApp Cloud API send operations
- WhatsApp webhook verification and incoming message handling
- Existing `/Booking`, `/Rooms`, `/Packages` and `/WhatsApp` routes
- Existing resort data and seed data

The original resort experience is still available at:

- `/Resort`
- `/business/paradise-palm-resort`

The root `/` is now the UplivaAI marketing website.

## New platform foundation

### Public

- UplivaAI marketing website
- Business registration
- Business type selection
- Loading screen / platform branding
- Reusable business website route: `/business/{slug}`
- Business-specific catalog, hot deals and enquiry form
- Demo testimonials are explicitly marked as sample/demo content

### Authorization

Two roles are implemented:

- `Admin`
- `BusinessOwner`

Admin can:

- review registrations
- approve/reject businesses
- edit business information
- manage content
- publish/unpublish business websites

Business owners can:

- register their business
- log in after admin approval
- edit their own business information
- manage their catalog and hot deals
- view their business website and enquiries

Business owners cannot approve or publish their own business.

## First-time setup

### 1. Configure the admin account with User Secrets

Do not put the admin password in source control.

From the project directory:

```powershell
dotnet user-secrets set "Admin:Name" "UplivaAI Administrator"
dotnet user-secrets set "Admin:Email" "your-admin-email@example.com"
dotnet user-secrets set "Admin:Password" "YOUR_STRONG_ADMIN_PASSWORD"
```

### 2. Configure the existing WhatsApp secrets

Use a newly generated Meta access token if an old token was exposed.

```powershell
dotnet user-secrets set "WhatsApp:AccessToken" "YOUR_META_ACCESS_TOKEN"
dotnet user-secrets set "WhatsApp:PhoneNumberId" "YOUR_PHONE_NUMBER_ID"
dotnet user-secrets set "WhatsApp:WebhookVerifyToken" "YOUR_WEBHOOK_VERIFY_TOKEN"
```

Never commit access tokens or passwords.

### 3. Run the application

The existing EF Core migrations are applied first. The new platform tables are then added idempotently by `PlatformSchemaInitializer`, so the existing resort migration is not rewritten.

## Business lifecycle

```text
Business Owner
     |
     v
Registration
     |
     v
Pending
     |
     | Admin review
     v
Approved
     |
     | Business owner can configure content
     v
Ready for publishing
     |
     | Admin only
     v
Published
     |
     v
/business/{slug}
```

## Reusable architecture

The same application is intended to support many businesses:

```text
UplivaAI
 |
 +-- Paradise Palm Resort
 +-- Grocery business
 +-- Hardware business
 +-- Furniture business
 +-- Salon
 +-- Restaurant
 +-- Pathology / Diagnostic
 +-- School
 +-- Transportation
 +-- ...
```

No separate application is required for each business. Business-specific content is stored with a `BusinessId` and displayed through the reusable business website route.

## WhatsApp roadmap

The current WhatsApp Cloud API implementation remains application-level so the working demo is not disrupted.

`BusinessWhatsAppSettings` has been added as the foundation for business-specific WhatsApp configuration. Production secrets should eventually be stored in a secure secret manager rather than plain database columns.

Recommended next production stages:

1. Complete business-specific WhatsApp number onboarding.
2. Map incoming `phone_number_id` to a Business.
3. Resolve the business before processing a webhook.
4. Send replies using that business's WhatsApp configuration.
5. Store message audit records.
6. Add template management and delivery status handling.

## Security roadmap

Before production:

- Move all WhatsApp tokens to a secret manager.
- Use HTTPS with a stable public domain.
- Add rate limiting to registration/login/webhook endpoints.
- Add account lockout and password reset.
- Add email/phone verification where required.
- Add audit logs for admin approval/publishing actions.
- Add stronger tenant isolation and authorization tests.
- Add database backups and monitoring.

## Important demo-content rule

The prototype may contain clearly labeled demo testimonials. Do not present invented testimonials, customer names, operating regions or endorsements as real customer evidence. Replace demo content with verified customer feedback before public marketing use.

## Local administrator login

The admin account is intentionally not hard-coded. Configure it with User Secrets before signing in:

```powershell
dotnet user-secrets set "Admin:Name" "UplivaAI Administrator"
dotnet user-secrets set "Admin:Email" "your-admin-email@example.com"
dotnet user-secrets set "Admin:Password" "your-strong-password"
```

Then restart the application. Verify the setting names with:

```powershell
dotnet user-secrets list
```

Do not commit passwords or WhatsApp access tokens to source control.

### Marketing homepage
The public homepage is intentionally concise: three scrolling sections focused on the WhatsApp lead problem, what UplivaAI provides, and business registration. A 15-second advertisement is stored under `wwwroot/videos/uplivaai-15s-ad.mp4`.

## Marketing v8 refinements
- Public marketing homepage is limited to three focused sections: lead hook, offering + representative feedback, and registration.
- Friendly `/login` and `/register` routes were added without removing the existing controller routes.
- Marketing login links now use `/login` explicitly.
- The 15-second marketing video includes voiceover + subtle background audio.
- The video copy no longer mentions Ranchi and is written for scalable local-business positioning.
- Representative feedback is clearly labelled as illustrative and should be replaced with verified customer reviews before public launch.
- No database migration is required for these marketing-only changes.

## Login route verification

The platform login is explicitly mapped to both of these URLs:

- `https://localhost:7248/login`
- `https://localhost:7248/Account/Login`

Cookie authentication redirects unauthenticated users to `/login`. The marketing navigation also uses `/login`.

Before testing login, configure the administrator with User Secrets:

```powershell
dotnet user-secrets set "Admin:Name" "UplivaAI Administrator"
dotnet user-secrets set "Admin:Email" "your-email@example.com"
dotnet user-secrets set "Admin:Password" "YOUR_STRONG_PASSWORD"
```

Then restart the application. Do not commit these secrets.


## Refreshed marketing video
The 20-second UplivaAI advertisement has been refreshed with a cleaner, friendly human-cartoon business owner, clearer WhatsApp-to-lead storytelling, and an original soft upbeat background music track. The MP4 includes H.264 video and AAC stereo audio.


## Login routing
The application uses explicit conventional routes for login: `/login` and `/Account/Login`, both handled by `AccountController.Login`. The marketing navigation and login form use MVC tag helpers instead of hard-coded login URLs.

## Login setup

The application uses a dedicated authentication cookie named `.UplivaAI.Auth` so cookies from older development builds do not interfere with the current login flow.

Configure the local administrator with User Secrets:

```powershell
dotnet user-secrets set "Admin:Name" "UplivaAI Administrator"
dotnet user-secrets set "Admin:Email" "your-email@example.com"
dotnet user-secrets set "Admin:Password" "YOUR_STRONG_PASSWORD"
```

The public login URL is:

`https://localhost:7248/login`

`/Account/Login` remains available as a compatibility URL.

The admin seeder creates or updates the configured admin account from User Secrets. No database migration is required for this login fix.

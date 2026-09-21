# UplivaResortBooking — ASP.NET Core MVC .NET 10 + WhatsApp

This version adds a WhatsApp-first resort booking layer on top of the resort website/database.

## Included

- Resort website
- SQL Server / LocalDB with EF Core
- Rooms, packages, bookings and resort information
- Room availability API
- WhatsApp Cloud API service
- WhatsApp welcome interactive list
- Promotional image + caption message
- WhatsApp webhook
- Incoming "Hi" handling
- Interactive selection handling
- Structured `ILogger` logging
- WhatsApp test page at `/WhatsApp`

## 1. Configure Meta WhatsApp

Do NOT put the Meta access token in source control.

For local development, initialize User Secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "WhatsApp:PhoneNumberId" "YOUR_PHONE_NUMBER_ID"
dotnet user-secrets set "WhatsApp:AccessToken" "YOUR_NEW_ACCESS_TOKEN"
dotnet user-secrets set "WhatsApp:WebhookVerifyToken" "YOUR_WEBHOOK_VERIFY_TOKEN"
```

The access token should be a newly generated token if an old token was previously exposed.

## 2. Database

```powershell
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Then run:

```powershell
dotnet run
```

## 3. Test WhatsApp from the website

Open:

```text
/WhatsApp
```

Enter the Meta test recipient number and click:

```text
Send Resort WhatsApp Menu
```

You should receive:

```text
🏝️ Paradise Palm Resort

Welcome! 👋

[Explore Resort]
```

with options:

- Book a Room
- Check Availability
- View Packages
- Resort Location
- Contact Resort

You can also send the resort promotional image.

## 4. Webhook

Application endpoint:

```text
GET  /webhooks/whatsapp
POST /webhooks/whatsapp
```

Meta needs a public HTTPS callback during local testing.

Example:

```text
https://YOUR-PUBLIC-TUNNEL/webhooks/whatsapp
```

Do not configure Meta with `https://localhost:7248` because Meta cannot reach your local machine directly.

## 5. Current flow

```text
Customer: Hi
       ↓
.NET webhook
       ↓
Welcome interactive list
       ↓
Customer selects Book a Room
       ↓
.NET flow service
       ↓
Booking website / booking logic
```

The next implementation step is to make date selection, guest selection and room selection fully interactive inside WhatsApp, then create the booking directly in SQL Server.

## 6. Logging

The application uses `ILogger`.

Look in Visual Studio:

```text
View → Output
```

Select:

```text
Debug
```

or run from the terminal and watch the console.

Examples:

```text
WhatsApp message received
Processing WhatsApp selection
Checking room availability
Booking created successfully
WhatsApp API error
```

Access tokens are intentionally not logged.

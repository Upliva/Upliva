# WhatsApp-first database

The current UplivaAI MVP is intentionally scoped to:

- marketing page / chatbot lead capture
- admin authentication
- lead qualification and business creation
- WhatsApp Business connection
- dynamic WhatsApp catalog
- WhatsApp product selection/ranking
- WhatsApp preview
- inbound/outbound WhatsApp message logging
- WhatsApp enquiries
- customer follow-up tracking

Website/custom-domain tables are intentionally deferred.

## Entity Framework workflow

This source currently contains no EF migrations. In Visual Studio Package Manager Console:

```powershell
Add-Migration WhatsAppFirstMvp -OutputDir Migrations
Update-Database
```

Or CLI:

```bash
dotnet ef migrations add WhatsAppFirstMvp --output-dir Migrations
dotnet ef database update
```

`WhatsAppMvpSchema.sql` is provided as a SQL Server reference/fresh-database script. Do not run it on an existing database without a backup because it is a create-schema script.

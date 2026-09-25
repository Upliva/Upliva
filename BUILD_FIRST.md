# Build and database setup

1. Open `UplivaAI.sln` in Visual Studio.
2. Build Solution (Ctrl+Shift+B). The current project intentionally excludes the future website Razor source.
3. Configure the SQL Server connection in `appsettings.json`; default database is `UplivaAIDb`.
4. After a successful build, create the first EF Core migration:

```powershell
Add-Migration WhatsAppFirstMvp -OutputDir Migrations
Update-Database
```

5. Configure Admin:Email and Admin:Password with User Secrets before running.

The future website source is in `FutureWebsite_Source_For_Later.zip` and is not part of the current WhatsApp-first MVP build.

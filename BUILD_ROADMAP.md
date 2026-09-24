# UplivaAI MVP Build Roadmap

## Current baseline
- ASP.NET Core MVC / .NET 10
- SQL Server + EF Core
- Existing application functionality preserved
- Marketing chatbot: Name -> Business Type -> WhatsApp
- Marketing lead + website visit tracking
- Browser voice prompts using Web Speech API
- No audio files and no voice database fields

## Important migration rule
This source package intentionally does not include a Migrations folder. If the local development database and Migrations folder were intentionally deleted, rebuild first and then create ONE fresh migration from the current model:

```powershell
Add-Migration InitialCreate
Update-Database
```

Do not delete the database again after this unless you intentionally want another development reset.

## Build validation
1. Clean Solution
2. Rebuild Solution
3. Confirm 0 compile errors
4. If the build succeeds, run `Add-Migration InitialCreate` only when there is no existing migration history in the project.
5. Run `Update-Database`.

## Marketing chatbot smoke test
1. Open the public marketing page.
2. Click Let's talk.
3. Voice says the welcome message when browser speech synthesis is supported.
4. Enter name.
5. Select business type.
6. Enter a valid WhatsApp number.
7. Submit.
8. Confirm thank-you message and lead record.
9. Admin -> Marketing shows the lead and visit counts.
10. Export CSV and confirm the lead is present.

## Voice behavior
Voice is progressive enhancement. If browser speech synthesis is unavailable, the chatbot remains fully usable by text. No microphone permission is requested.

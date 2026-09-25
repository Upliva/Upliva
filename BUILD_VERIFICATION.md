# Build Verification

This package is the WhatsApp-first UplivaAI source. It intentionally excludes the old website-specific MVC models/views from the active project.

Expected active flow:
Marketing -> Lead -> Admin -> Business -> WhatsApp Business -> Dynamic Catalog -> WhatsApp Selection/Ranking -> Preview -> WhatsApp API/Webhook.

Before database migration:
1. Open UplivaAI.sln in Visual Studio.
2. Build > Clean Solution.
3. Build > Rebuild Solution.
4. Confirm 0 errors.
5. Only then create the EF migration if the database is empty.

This environment does not include the .NET SDK, so the source was statically checked but not compiled here.

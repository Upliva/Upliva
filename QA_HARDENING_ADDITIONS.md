# UplivaAI - Initial Development Hardening Additions

These additions are layered on top of the QA fixes and are intentionally isolated from existing business functionality.

## 1. Audit logging

Persisted in `AuditLogs` through EF Core migration `20260924080000_AddAuditLogging`.

Recorded events include:
- business approval/rejection
- business publish/unpublish
- business profile changes
- website content changes
- catalog add/update/delete
- WhatsApp Top Pick changes
- offer/deal add/publish

Each audit row records action, entity, business, authenticated user, role, correlation ID, details and UTC timestamp.

## 2. Centralized exception handling

`Middleware/ExceptionHandlingMiddleware.cs` catches unhandled request exceptions in one place.

- MVC requests receive a generic error response with a correlation ID.
- `/api/*` and `/webhooks/*` receive a JSON 500 response with the correlation ID.
- The exception and request information are logged centrally.

## 3. Correlation IDs and structured logging

`Middleware/CorrelationIdMiddleware.cs` accepts or creates `X-Correlation-ID` and returns it on the response.

The request scope includes correlation ID and request path. Request completion is logged with method, path, status code and duration. Console logging uses JSON output for structured log ingestion.

## 4. Caching

`BusinessCacheService` uses the built-in `IMemoryCache`.

- Published business website models: 5-minute absolute / 2-minute sliding cache.
- WhatsApp Top 6 catalog: 2-minute absolute / 1-minute sliding cache.
- Business mutations explicitly invalidate affected cache entries.

This is an in-process cache suitable for the initial single-instance deployment. A distributed cache can be introduced later if UplivaAI runs on multiple application instances.

## Database update

Run the existing EF Core migration workflow:

```powershell
Update-Database
```

or:

```bash
dotnet ef database update
```

No existing migration was changed; a new migration was added.

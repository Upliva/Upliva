# UplivaAI Phase 1 Architecture

```text
Customer
  -> Public Business Page
     -> Search / Products / Services / Offers
     -> Call -> CallEvent -> Owner Dashboard
     -> Enquiry -> SQL -> Notification Queue -> Email -> Owner

Business Registration
  -> RegistrationLead
  -> Admin Review
  -> Business
  -> Business Owner Account
  -> Mobile + Password
  -> Owner Dashboard

Owner Dashboard
  -> Profile / Catalog / Offers / Leads
  -> IBlobStorageService -> Azure Blob Storage

Admin Portal
  -> Registrations / Businesses / Templates / Enquiries / Logs

Operations
  -> Correlation ID
  -> ErrorLog / IntegrationLog / NotificationLog / AuditLog
  -> SQL Server

Optional later integration
  -> IWhatsAppMessagingService -> Gupshup -> Meta WhatsApp
```

Core principle: Upliva's business/lead system does not depend on Gupshup. Azure, Email and Gupshup are replaceable infrastructure integrations behind interfaces.

/*
 UplivaAI - WhatsApp-first MVP schema
 -------------------------------------
 Source of truth: SQL Server.
 Dynamic catalog attributes are stored as JSON in BusinessCatalogItems.CustomAttributesJson.
 Catalog templates remain versioned JSON files under /CatalogTemplates for now.
 Website/custom-domain tables are intentionally not included in this MVP schema.
*/

CREATE TABLE PlatformUsers
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformUsers PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(200) NOT NULL,
    PhoneNumber NVARCHAR(30) NOT NULL DEFAULT '',
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(40) NOT NULL,
    BusinessId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE UNIQUE INDEX UX_PlatformUsers_Email ON PlatformUsers(Email);

CREATE TABLE Businesses
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Businesses PRIMARY KEY,
    Name NVARCHAR(180) NOT NULL,
    Slug NVARCHAR(80) NOT NULL,
    BusinessType NVARCHAR(80) NOT NULL,
    CatalogTemplateKey NVARCHAR(80) NOT NULL DEFAULT 'generic',
    Status NVARCHAR(40) NOT NULL DEFAULT 'Pending',
    ServicePlan NVARCHAR(40) NOT NULL DEFAULT '',
    OwnerName NVARCHAR(150) NOT NULL DEFAULT '',
    Email NVARCHAR(200) NOT NULL DEFAULT '',
    PhoneNumber NVARCHAR(30) NOT NULL DEFAULT '',
    WhatsAppNumber NVARCHAR(30) NOT NULL DEFAULT '',
    Address NVARCHAR(300) NOT NULL DEFAULT '',
    City NVARCHAR(100) NOT NULL DEFAULT '',
    State NVARCHAR(100) NOT NULL DEFAULT '',
    PostalCode NVARCHAR(20) NOT NULL DEFAULT '',
    Country NVARCHAR(100) NOT NULL DEFAULT 'India',
    BusinessHours NVARCHAR(500) NOT NULL DEFAULT '',
    Tagline NVARCHAR(250) NOT NULL DEFAULT '',
    Description NVARCHAR(2000) NOT NULL DEFAULT '',
    LogoUrl NVARCHAR(500) NOT NULL DEFAULT '',
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ApprovedAtUtc DATETIME2 NULL
);
CREATE UNIQUE INDEX UX_Businesses_Slug ON Businesses(Slug);
CREATE UNIQUE INDEX UX_Businesses_WhatsAppNumber ON Businesses(WhatsAppNumber) WHERE WhatsAppNumber IS NOT NULL AND WhatsAppNumber <> '';
ALTER TABLE PlatformUsers ADD CONSTRAINT FK_PlatformUsers_Businesses FOREIGN KEY (BusinessId) REFERENCES Businesses(Id);

CREATE TABLE ChatbotLeads
(
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatbotLeads PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    BusinessType NVARCHAR(80) NOT NULL,
    WhatsAppNumber NVARCHAR(30) NOT NULL,
    Source NVARCHAR(50) NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    SelectedPlan NVARCHAR(40) NOT NULL DEFAULT '',
    AdminNotes NVARCHAR(1000) NOT NULL DEFAULT '',
    VisitorId NVARCHAR(100) NOT NULL DEFAULT '',
    ConvertedBusinessId INT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ContactedAtUtc DATETIME2 NULL,
    ConfirmedAtUtc DATETIME2 NULL,
    ConvertedAtUtc DATETIME2 NULL
);
CREATE INDEX IX_ChatbotLeads_CreatedAtUtc ON ChatbotLeads(CreatedAtUtc);
CREATE INDEX IX_ChatbotLeads_WhatsAppNumber ON ChatbotLeads(WhatsAppNumber);
CREATE INDEX IX_ChatbotLeads_Status ON ChatbotLeads(Status);
CREATE INDEX IX_ChatbotLeads_ConvertedBusinessId ON ChatbotLeads(ConvertedBusinessId);
ALTER TABLE ChatbotLeads ADD CONSTRAINT FK_ChatbotLeads_Businesses FOREIGN KEY (ConvertedBusinessId) REFERENCES Businesses(Id);

CREATE TABLE BusinessWhatsAppSettings
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessWhatsAppSettings PRIMARY KEY,
    BusinessId INT NOT NULL,
    WabaId NVARCHAR(100) NOT NULL DEFAULT '',
    DisplayName NVARCHAR(150) NOT NULL DEFAULT '',
    AboutText NVARCHAR(500) NOT NULL DEFAULT '',
    BusinessCategory NVARCHAR(100) NOT NULL DEFAULT '',
    WelcomeMessage NVARCHAR(500) NOT NULL DEFAULT '',
    PhoneNumberId NVARCHAR(100) NOT NULL DEFAULT '',
    AccessToken NVARCHAR(MAX) NOT NULL DEFAULT '',
    WebhookVerifyToken NVARCHAR(100) NOT NULL DEFAULT '',
    GraphApiVersion NVARCHAR(20) NOT NULL DEFAULT 'v26.0',
    IsEnabled BIT NOT NULL DEFAULT 0,
    FeaturedProductLimit INT NOT NULL DEFAULT 6,
    CONSTRAINT FK_BusinessWhatsAppSettings_Businesses FOREIGN KEY (BusinessId) REFERENCES Businesses(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX UX_BusinessWhatsAppSettings_BusinessId ON BusinessWhatsAppSettings(BusinessId);
CREATE UNIQUE INDEX UX_BusinessWhatsAppSettings_PhoneNumberId ON BusinessWhatsAppSettings(PhoneNumberId) WHERE PhoneNumberId IS NOT NULL AND PhoneNumberId <> '';

CREATE TABLE BusinessCatalogItems
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessCatalogItems PRIMARY KEY,
    BusinessId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Brand NVARCHAR(100) NOT NULL DEFAULT '',
    Model NVARCHAR(100) NOT NULL DEFAULT '',
    SKU NVARCHAR(80) NOT NULL DEFAULT '',
    Category NVARCHAR(80) NOT NULL DEFAULT '',
    PriceText NVARCHAR(100) NOT NULL DEFAULT '',
    OriginalPriceText NVARCHAR(100) NOT NULL DEFAULT '',
    DiscountText NVARCHAR(80) NOT NULL DEFAULT '',
    ImageUrl NVARCHAR(500) NOT NULL DEFAULT '',
    ShortDescription NVARCHAR(300) NOT NULL DEFAULT '',
    Description NVARCHAR(1000) NOT NULL DEFAULT '',
    CustomAttributesJson NVARCHAR(MAX) NOT NULL DEFAULT '{}',
    StockStatus NVARCHAR(80) NOT NULL DEFAULT '',
    Rating DECIMAL(3,2) NULL,
    ReviewCount INT NOT NULL DEFAULT 0,
    IsWhatsAppTopPick BIT NOT NULL DEFAULT 0,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_BusinessCatalogItems_Businesses FOREIGN KEY (BusinessId) REFERENCES Businesses(Id) ON DELETE CASCADE
);
CREATE INDEX IX_BusinessCatalogItems_BusinessId ON BusinessCatalogItems(BusinessId);
CREATE INDEX IX_BusinessCatalogItems_WhatsAppOrder ON BusinessCatalogItems(BusinessId, IsActive, IsWhatsAppTopPick, SortOrder);
CREATE INDEX IX_BusinessCatalogItems_Category ON BusinessCatalogItems(BusinessId, Category);

CREATE TABLE BusinessOffers
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessOffers PRIMARY KEY,
    BusinessId INT NOT NULL,
    Title NVARCHAR(180) NOT NULL,
    Description NVARCHAR(1000) NOT NULL DEFAULT '',
    ImageUrl NVARCHAR(500) NOT NULL DEFAULT '',
    DiscountText NVARCHAR(80) NOT NULL DEFAULT '',
    StartsOn DATETIME2 NULL,
    EndsOn DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_BusinessOffers_Businesses FOREIGN KEY (BusinessId) REFERENCES Businesses(Id) ON DELETE CASCADE
);
CREATE INDEX IX_BusinessOffers_BusinessActive ON BusinessOffers(BusinessId, IsActive);

CREATE TABLE BusinessEnquiries
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessEnquiries PRIMARY KEY,
    BusinessId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    PhoneNumber NVARCHAR(30) NOT NULL,
    Email NVARCHAR(200) NOT NULL DEFAULT '',
    Message NVARCHAR(2000) NOT NULL,
    Status NVARCHAR(40) NOT NULL DEFAULT 'New',
    Source NVARCHAR(30) NOT NULL DEFAULT 'WhatsApp',
    WhatsAppMessageId NVARCHAR(150) NOT NULL DEFAULT '',
    CatalogItemId INT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_BusinessEnquiries_Businesses FOREIGN KEY (BusinessId) REFERENCES Businesses(Id) ON DELETE CASCADE,
    CONSTRAINT FK_BusinessEnquiries_CatalogItems FOREIGN KEY (CatalogItemId) REFERENCES BusinessCatalogItems(Id) ON DELETE SET NULL
);
CREATE INDEX IX_BusinessEnquiries_BusinessCreated ON BusinessEnquiries(BusinessId, CreatedAtUtc);
CREATE INDEX IX_BusinessEnquiries_WhatsAppMessageId ON BusinessEnquiries(WhatsAppMessageId);

CREATE TABLE WhatsAppMessageLogs
(
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WhatsAppMessageLogs PRIMARY KEY,
    BusinessId INT NOT NULL,
    Direction NVARCHAR(20) NOT NULL,
    MessageType NVARCHAR(40) NOT NULL,
    CustomerPhoneNumber NVARCHAR(30) NOT NULL DEFAULT '',
    CustomerName NVARCHAR(150) NOT NULL DEFAULT '',
    ExternalMessageId NVARCHAR(200) NOT NULL DEFAULT '',
    SelectionId NVARCHAR(100) NOT NULL DEFAULT '',
    MessageText NVARCHAR(5000) NOT NULL DEFAULT '',
    DeliveryStatus NVARCHAR(40) NOT NULL DEFAULT 'Received',
    ErrorMessage NVARCHAR(2000) NOT NULL DEFAULT '',
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_WhatsAppMessageLogs_Businesses FOREIGN KEY (BusinessId) REFERENCES Businesses(Id) ON DELETE CASCADE
);
CREATE INDEX IX_WhatsAppMessageLogs_BusinessCreated ON WhatsAppMessageLogs(BusinessId, CreatedAtUtc);
CREATE UNIQUE INDEX UX_WhatsAppMessageLogs_BusinessExternalId ON WhatsAppMessageLogs(BusinessId, ExternalMessageId) WHERE ExternalMessageId IS NOT NULL AND ExternalMessageId <> '';
CREATE INDEX IX_WhatsAppMessageLogs_Customer ON WhatsAppMessageLogs(BusinessId, CustomerPhoneNumber);

CREATE TABLE AuditLogs
(
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    Action NVARCHAR(80) NOT NULL,
    EntityType NVARCHAR(80) NOT NULL DEFAULT '',
    EntityId NVARCHAR(100) NOT NULL DEFAULT '',
    BusinessId INT NULL,
    UserId INT NULL,
    UserEmail NVARCHAR(200) NOT NULL DEFAULT '',
    UserRole NVARCHAR(40) NOT NULL DEFAULT '',
    CorrelationId NVARCHAR(100) NOT NULL,
    Details NVARCHAR(4000) NOT NULL DEFAULT '',
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditLogs_Businesses FOREIGN KEY (BusinessId) REFERENCES Businesses(Id) ON DELETE SET NULL,
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES PlatformUsers(Id) ON DELETE SET NULL
);
CREATE INDEX IX_AuditLogs_BusinessCreated ON AuditLogs(BusinessId, CreatedAtUtc);
CREATE INDEX IX_AuditLogs_CorrelationId ON AuditLogs(CorrelationId);

CREATE TABLE ErrorLogs
(
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ErrorLogs PRIMARY KEY,
    CorrelationId NVARCHAR(100) NOT NULL,
    ExceptionType NVARCHAR(100) NOT NULL,
    Message NVARCHAR(2000) NOT NULL,
    StackTrace NVARCHAR(10000) NOT NULL DEFAULT '',
    HttpMethod NVARCHAR(10) NOT NULL DEFAULT '',
    RequestPath NVARCHAR(500) NOT NULL DEFAULT '',
    UserEmail NVARCHAR(200) NOT NULL DEFAULT '',
    UserId INT NULL,
    BusinessId INT NULL,
    StatusCode INT NOT NULL DEFAULT 500,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ErrorLogs_Businesses FOREIGN KEY (BusinessId) REFERENCES Businesses(Id) ON DELETE SET NULL,
    CONSTRAINT FK_ErrorLogs_Users FOREIGN KEY (UserId) REFERENCES PlatformUsers(Id) ON DELETE SET NULL
);
CREATE INDEX IX_ErrorLogs_BusinessCreated ON ErrorLogs(BusinessId, CreatedAtUtc);
CREATE INDEX IX_ErrorLogs_CorrelationId ON ErrorLogs(CorrelationId);

CREATE TABLE PlatformVisits
(
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformVisits PRIMARY KEY,
    VisitorId NVARCHAR(100) NOT NULL,
    Path NVARCHAR(250) NOT NULL DEFAULT '/',
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_PlatformVisits_CreatedAtUtc ON PlatformVisits(CreatedAtUtc);
CREATE INDEX IX_PlatformVisits_VisitorId ON PlatformVisits(VisitorId);
GO

CREATE TABLE BusinessFollowUps
(
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessFollowUps PRIMARY KEY,
    BusinessId INT NOT NULL,
    EnquiryId INT NULL,
    CatalogItemId INT NULL,
    CustomerName NVARCHAR(150) NOT NULL,
    CustomerPhoneNumber NVARCHAR(30) NOT NULL,
    Notes NVARCHAR(1000) NOT NULL DEFAULT '',
    Status NVARCHAR(40) NOT NULL DEFAULT 'Pending',
    DueAtUtc DATETIME2 NULL,
    CompletedAtUtc DATETIME2 NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_BusinessFollowUps_Businesses FOREIGN KEY (BusinessId) REFERENCES Businesses(Id),
    CONSTRAINT FK_BusinessFollowUps_Enquiries FOREIGN KEY (EnquiryId) REFERENCES BusinessEnquiries(Id),
    CONSTRAINT FK_BusinessFollowUps_CatalogItems FOREIGN KEY (CatalogItemId) REFERENCES BusinessCatalogItems(Id)
);
CREATE INDEX IX_BusinessFollowUps_BusinessStatusDue ON BusinessFollowUps(BusinessId, Status, DueAtUtc);
CREATE INDEX IX_BusinessFollowUps_EnquiryId ON BusinessFollowUps(EnquiryId);

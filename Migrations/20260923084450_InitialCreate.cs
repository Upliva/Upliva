using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UplivaAI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PriceText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OriginalPriceText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiscountText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StockStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    IsWhatsAppTopPick = table.Column<bool>(type: "bit", nullable: false),
                    ShowOnWebsite = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEnquiries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEnquiries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BusinessType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WhatsAppNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BusinessHours = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Tagline = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HeroImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DiscountText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StartsOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndsOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessTestimonials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessTestimonials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessWhatsAppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    WabaId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumberId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WebhookVerifyToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GraphApiVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessWhatsAppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebsiteConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    WebsiteTitle = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    MetaDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    HeroTitle = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    HeroSubtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AboutTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AboutContent = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    WhyChooseUsTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WhyChooseUsContent = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    ServicesTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ServicesContent = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    CallToActionTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CallToActionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContactIntro = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FooterText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccentColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShowOffers = table.Column<bool>(type: "bit", nullable: false),
                    ShowTestimonials = table.Column<bool>(type: "bit", nullable: false),
                    ShowCatalog = table.Column<bool>(type: "bit", nullable: false),
                    ShowWhatsApp = table.Column<bool>(type: "bit", nullable: false),
                    ShowWhyChooseUs = table.Column<bool>(type: "bit", nullable: false),
                    ShowServices = table.Column<bool>(type: "bit", nullable: false),
                    ShowCallToAction = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCatalogItems_BusinessId",
                table: "BusinessCatalogItems",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCatalogItems_BusinessId_IsActive_ShowOnWebsite",
                table: "BusinessCatalogItems",
                columns: new[] { "BusinessId", "IsActive", "ShowOnWebsite" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEnquiries_BusinessId",
                table: "BusinessEnquiries",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Slug",
                table: "Businesses",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOffers_BusinessId",
                table: "BusinessOffers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessTestimonials_BusinessId",
                table: "BusinessTestimonials",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessWhatsAppSettings_BusinessId",
                table: "BusinessWhatsAppSettings",
                column: "BusinessId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUsers_Email",
                table: "PlatformUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteConfigurations_BusinessId",
                table: "WebsiteConfigurations",
                column: "BusinessId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessCatalogItems");

            migrationBuilder.DropTable(
                name: "BusinessEnquiries");

            migrationBuilder.DropTable(
                name: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessOffers");

            migrationBuilder.DropTable(
                name: "BusinessTestimonials");

            migrationBuilder.DropTable(
                name: "BusinessWhatsAppSettings");

            migrationBuilder.DropTable(
                name: "PlatformUsers");

            migrationBuilder.DropTable(
                name: "WebsiteConfigurations");
        }
    }
}

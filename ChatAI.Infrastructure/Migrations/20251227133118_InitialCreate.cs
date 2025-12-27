using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidationRule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPlatformAdmin = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RateLimitPerMinute = table.Column<int>(type: "int", nullable: false, defaultValue: 20),
                    RateLimitPerDay = table.Column<int>(type: "int", nullable: false, defaultValue: 1000),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SessionMetadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmbeddingReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PlanTier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Free"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CustomDomain = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "#667eea"),
                    MaxDocuments = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    MaxMonthlyMessages = table.Column<int>(type: "int", nullable: false, defaultValue: 1000),
                    CurrentDocumentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CurrentMonthMessages = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BillingPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_AdminUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsToolCall = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ToolName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToolArguments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToolResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmbeddingReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    TotalTokens = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VectorStorageMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "SQL"),
                    QdrantCollectionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EnableDocumentChunking = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ChunkSize = table.Column<int>(type: "int", nullable: false, defaultValue: 512),
                    ChunkOverlap = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    EnableChatHistory = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ChatHistoryRetentionDays = table.Column<int>(type: "int", nullable: false, defaultValue: 90),
                    EnableFeedback = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    WelcomeMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChatPlaceholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, defaultValue: "Ask me anything..."),
                    EnableTools = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Temperature = table.Column<double>(type: "float", nullable: false, defaultValue: 0.69999999999999996),
                    MaxTokens = table.Column<int>(type: "int", nullable: false, defaultValue: 1000),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminConfigurations_Category",
                table: "AdminConfigurations",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AdminConfigurations_IsActive",
                table: "AdminConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AdminConfigurations_Key",
                table: "AdminConfigurations",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Email",
                table: "AdminUsers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_IsActive",
                table: "AdminUsers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Tenant_Username",
                table: "AdminUsers",
                columns: new[] { "TenantId", "Username" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_TenantId",
                table: "AdminUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Username",
                table: "AdminUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_ClientId",
                table: "ApiKeys",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_CreatedAt",
                table: "ApiKeys",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_IsActive",
                table: "ApiKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Session_Time",
                table: "ChatMessages",
                columns: new[] { "SessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId",
                table: "ChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Tenant_Time",
                table: "ChatMessages",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_TenantId",
                table: "ChatMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Timestamp",
                table: "ChatMessages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_UserId",
                table: "ChatMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CreatedAt",
                table: "ChatSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_IsActive",
                table: "ChatSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_LastActivity",
                table: "ChatSessions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_Tenant_Created",
                table: "ChatSessions",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_TenantId",
                table: "ChatSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_Category",
                table: "KnowledgeDocuments",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_Category_Active",
                table: "KnowledgeDocuments",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_CreatedAt",
                table: "KnowledgeDocuments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_IsActive",
                table: "KnowledgeDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_Source",
                table: "KnowledgeDocuments",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_Tenant_Active",
                table: "KnowledgeDocuments",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_TenantId",
                table: "KnowledgeDocuments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageFeedbacks_CreatedAt",
                table: "MessageFeedbacks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MessageFeedbacks_MessageId",
                table: "MessageFeedbacks",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageFeedbacks_Rating",
                table: "MessageFeedbacks",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_MessageFeedbacks_SessionId",
                table: "MessageFeedbacks",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageFeedbacks_Tenant_Rating",
                table: "MessageFeedbacks",
                columns: new[] { "TenantId", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageFeedbacks_TenantId",
                table: "MessageFeedbacks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_AdminUserId",
                table: "Tenants",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CreatedAt",
                table: "Tenants",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CustomDomain",
                table: "Tenants",
                column: "CustomDomain",
                unique: true,
                filter: "[CustomDomain] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Email",
                table: "Tenants",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsActive",
                table: "Tenants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PlanTier",
                table: "Tenants",
                column: "PlanTier");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId",
                table: "TenantSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminConfigurations");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "KnowledgeDocuments");

            migrationBuilder.DropTable(
                name: "MessageFeedbacks");

            migrationBuilder.DropTable(
                name: "TenantSettings");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "AdminUsers");
        }
    }
}

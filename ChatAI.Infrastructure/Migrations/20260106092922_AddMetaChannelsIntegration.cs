using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaChannelsIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetaChannelConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WebhookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerifyTokenHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VerifyTokenPlain = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaAppId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MetaAppSecretEncrypted = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AccessTokenEncrypted = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TokenKeyVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FacebookPageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InstagramBusinessAccountId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WhatsAppPhoneNumberId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WhatsAppBusinessAccountId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastWebhookAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSendAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastErrorAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedSendCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TokenExpiryWarning = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TokenExpiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaChannelConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetaConversationMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalUserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChatSessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaConversationMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetaConversationMaps_MetaChannelConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "MetaChannelConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetaInboundDedupes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetaMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaInboundDedupes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetaInboundDedupes_MetaChannelConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "MetaChannelConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetaChannelConnections_FacebookPageId",
                table: "MetaChannelConnections",
                column: "FacebookPageId",
                unique: true,
                filter: "[FacebookPageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MetaChannelConnections_InstagramBusinessAccountId",
                table: "MetaChannelConnections",
                column: "InstagramBusinessAccountId",
                unique: true,
                filter: "[InstagramBusinessAccountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MetaChannelConnections_IsActive",
                table: "MetaChannelConnections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MetaChannelConnections_Tenant_Active",
                table: "MetaChannelConnections",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MetaChannelConnections_Tenant_Channel",
                table: "MetaChannelConnections",
                columns: new[] { "TenantId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_MetaChannelConnections_TenantId",
                table: "MetaChannelConnections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaChannelConnections_TokenExpiryWarning",
                table: "MetaChannelConnections",
                column: "TokenExpiryWarning");

            migrationBuilder.CreateIndex(
                name: "IX_MetaChannelConnections_WebhookId",
                table: "MetaChannelConnections",
                column: "WebhookId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetaChannelConnections_WhatsAppPhoneNumberId",
                table: "MetaChannelConnections",
                column: "WhatsAppPhoneNumberId",
                unique: true,
                filter: "[WhatsAppPhoneNumberId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MetaConversationMaps_ChatSessionId",
                table: "MetaConversationMaps",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaConversationMaps_Connection_ExternalUser",
                table: "MetaConversationMaps",
                columns: new[] { "ConnectionId", "ExternalUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetaConversationMaps_ConnectionId",
                table: "MetaConversationMaps",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaConversationMaps_LastActivityAt",
                table: "MetaConversationMaps",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_MetaInboundDedupes_Connection_Message",
                table: "MetaInboundDedupes",
                columns: new[] { "ConnectionId", "MetaMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetaInboundDedupes_ConnectionId",
                table: "MetaInboundDedupes",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaInboundDedupes_ReceivedAt",
                table: "MetaInboundDedupes",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetaConversationMaps");

            migrationBuilder.DropTable(
                name: "MetaInboundDedupes");

            migrationBuilder.DropTable(
                name: "MetaChannelConnections");
        }
    }
}

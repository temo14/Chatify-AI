using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaOAuthSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OAuthRefreshTokenEncrypted",
                table: "MetaChannelConnections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthScopes",
                table: "MetaChannelConnections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemUserId",
                table: "MetaChannelConnections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenSource",
                table: "MetaChannelConnections",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OAuthRefreshTokenEncrypted",
                table: "MetaChannelConnections");

            migrationBuilder.DropColumn(
                name: "OAuthScopes",
                table: "MetaChannelConnections");

            migrationBuilder.DropColumn(
                name: "SystemUserId",
                table: "MetaChannelConnections");

            migrationBuilder.DropColumn(
                name: "TokenSource",
                table: "MetaChannelConnections");
        }
    }
}

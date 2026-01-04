using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEnableChatHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableChatHistory",
                table: "TenantSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableChatHistory",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }
    }
}

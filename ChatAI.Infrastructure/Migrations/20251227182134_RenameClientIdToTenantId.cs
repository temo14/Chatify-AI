using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameClientIdToTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "ApiKeys",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKeys_ClientId",
                table: "ApiKeys",
                newName: "IX_ApiKeys_TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "ApiKeys",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKeys_TenantId",
                table: "ApiKeys",
                newName: "IX_ApiKeys_ClientId");
        }
    }
}

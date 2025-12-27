using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdminUserIdFromTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_AdminUsers_AdminUserId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_AdminUserId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AdminUserId",
                table: "Tenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdminUserId",
                table: "Tenants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_AdminUserId",
                table: "Tenants",
                column: "AdminUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_AdminUsers_AdminUserId",
                table: "Tenants",
                column: "AdminUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

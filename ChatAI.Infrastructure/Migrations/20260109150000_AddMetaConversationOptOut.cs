using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaConversationOptOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOptedOut",
                table: "MetaConversationMaps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OptedOutAt",
                table: "MetaConversationMaps",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OptedInAt",
                table: "MetaConversationMaps",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOptedOut",
                table: "MetaConversationMaps");

            migrationBuilder.DropColumn(
                name: "OptedOutAt",
                table: "MetaConversationMaps");

            migrationBuilder.DropColumn(
                name: "OptedInAt",
                table: "MetaConversationMaps");
        }
    }
}

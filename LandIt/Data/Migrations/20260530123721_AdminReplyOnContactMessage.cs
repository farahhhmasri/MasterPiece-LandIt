using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LandIt.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminReplyOnContactMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminReply",
                table: "ContactMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepliedAt",
                table: "ContactMessages",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminReply",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "RepliedAt",
                table: "ContactMessages");
        }
    }
}

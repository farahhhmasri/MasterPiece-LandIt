using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LandIt.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuestionRequestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "QuestionRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "level",
                table: "QuestionRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "QuestionRequests");

            migrationBuilder.DropColumn(
                name: "level",
                table: "QuestionRequests");
        }
    }
}

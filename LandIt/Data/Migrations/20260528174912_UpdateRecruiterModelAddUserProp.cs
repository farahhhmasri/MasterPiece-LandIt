using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LandIt.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRecruiterModelAddUserProp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Recruiters",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recruiters_UserId",
                table: "Recruiters",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recruiters_AspNetUsers_UserId",
                table: "Recruiters",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recruiters_AspNetUsers_UserId",
                table: "Recruiters");

            migrationBuilder.DropIndex(
                name: "IX_Recruiters_UserId",
                table: "Recruiters");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Recruiters");
        }
    }
}

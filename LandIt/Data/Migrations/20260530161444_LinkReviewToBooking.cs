using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LandIt.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkReviewToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_RecruiterReviews_RecruiterReviewId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_RecruiterReviewId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RecruiterReviewId",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "BookingId",
                table: "RecruiterReviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterReviews_BookingId",
                table: "RecruiterReviews",
                column: "BookingId",
                unique: true,
                filter: "[BookingId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_RecruiterReviews_Bookings_BookingId",
                table: "RecruiterReviews",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecruiterReviews_Bookings_BookingId",
                table: "RecruiterReviews");

            migrationBuilder.DropIndex(
                name: "IX_RecruiterReviews_BookingId",
                table: "RecruiterReviews");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "RecruiterReviews");

            migrationBuilder.AddColumn<int>(
                name: "RecruiterReviewId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RecruiterReviewId",
                table: "Bookings",
                column: "RecruiterReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_RecruiterReviews_RecruiterReviewId",
                table: "Bookings",
                column: "RecruiterReviewId",
                principalTable: "RecruiterReviews",
                principalColumn: "Id");
        }
    }
}

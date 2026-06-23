using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LandIt.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTestimonialModelRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Testimonials",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Testimonials");
        }
    }
}

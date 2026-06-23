using System.ComponentModel.DataAnnotations;

namespace LandIt.Models
{
    public class Testimonial
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public int? BookingId { get; set; } // Which booking it came from 
        public Booking? Booking { get; set; }

        public string Content { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; } = 5; // 1-5 stars
        public bool IsApproved { get; set; } = false; // admin approves before showing

        public TestimonialSource? Source { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
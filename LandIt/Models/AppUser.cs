using Microsoft.AspNetCore.Identity;


namespace LandIt.Models
{
    public class AppUser:IdentityUser
    {
        public string? Photo { get; set; }
        public string FullName { get; set; }
        public Region Region { get; set; }
        public bool IsRecruiter { get; set; } = false;
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Booking> Bookings { get; set; }
        public ICollection<Resume> Resumes { get; set; }
        public ICollection<RecruiterReview> RecruiterReviews { get; set; }
        public ICollection<Testimonial> Testimonials { get; set; }
        public ICollection<QuestionRequest> QuestionRequests { get; set; }
        public ICollection<GeneratedResume> GeneratedResumes { get; set; }
    }
}

namespace LandIt.Models
{
    public class Recruiter
    {
        public int Id { get; set; }

        // OPTIONAL link to system user
        public string? UserId { get; set; }
        public AppUser? User { get; set; }
        public string FullName { get; set; }
        public string Company { get; set; }
        public string Title { get; set; }
        public Region Region { get; set; }
        public decimal HourlyRate { get; set; }
        public string? linkedInURL { get; set; }
        public string Skills { get; set; }
        public RecruiterStatus Status { get; set; } = RecruiterStatus.Pending;
        public ICollection<Booking>? Bookings { get; set; } = new List<Booking>();
        public ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
        public ICollection<RecruiterReview> Reviews { get; set; } = new List<RecruiterReview>();
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<RecruiterAvailability> Availabilities { get; set; }
    }
}

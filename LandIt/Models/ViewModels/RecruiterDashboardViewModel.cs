using LandIt.Models;

namespace LandIt.Models.ViewModels;

public class RecruiterDashboardViewModel
{
    public Recruiter Recruiter { get; set; }

    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ActiveSlots { get; set; }
    public int ReviewsCount { get; set; }
    public double AverageRating { get; set; }

    public List<Booking> UpcomingBookings { get; set; } = new();

    // Other recruiters to browse and book
    public List<Recruiter> OtherRecruiters { get; set; } = new();
    public string? Search { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LandIt.Models;

public class TimeSlot
{
    public int Id { get; set; }

    [Required]
    public int RecruiterId { get; set; }

    public virtual Recruiter? Recruiter { get; set; }
    public int? RecruiterAvailabilityId { get; set; }
    public virtual RecruiterAvailability? RecruiterAvailability { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public bool IsBooked { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Booking? Booking { get; set; }

    [NotMapped]
    public TimeSpan Duration => EndTime - StartTime;

    
    public DateTime ToLocalTime(string ianaTimeZone)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZone);
        return TimeZoneInfo.ConvertTimeFromUtc(StartTime, tz);
    }
}
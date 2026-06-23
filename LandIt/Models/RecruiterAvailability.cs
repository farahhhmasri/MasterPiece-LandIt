using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LandIt.Models;

public class RecruiterAvailability
{
    public int Id { get; set; }

    [Required]
    public int RecruiterId { get; set; }

    public virtual Recruiter? Recruiter { get; set; }

    [Required]
    [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 (Sunday) and 6 (Saturday).")]
    public int DayOfWeek { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    [MaxLength(100)]
    public string TimeZone { get; set; } = "UTC";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string DayName => ((DayOfWeek)DayOfWeek).ToString();

    [NotMapped]
    public TimeSpan Duration => EndTime.ToTimeSpan() - StartTime.ToTimeSpan();

    public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
}
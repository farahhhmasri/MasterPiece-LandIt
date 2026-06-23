using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LandIt.Models;

public class Booking
{
    public int Id { get; set; }


    [Required]
    public string UserId { get; set; } = string.Empty;   // FK → AspNetUsers.Id (string)

    [ForeignKey(nameof(UserId))]
    public virtual AppUser? User { get; set; }


    [Required]
    public int RecruiterId { get; set; }

    public virtual Recruiter? Recruiter { get; set; }


    [Required]
    public int TimeSlotId { get; set; }

    public virtual TimeSlot? TimeSlot { get; set; }

    // Session context

    [MaxLength(150)]
    public string? JobTitle { get; set; }

    [MaxLength(150)]
    public string? CompanyTarget { get; set; }

    public string? CandidateNotes { get; set; }


    [MaxLength(500)]
    public string? MeetingUrl { get; set; }

   public string? Notes { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Payment? Payment { get; set; }
    public virtual RecruiterReview? RecruiterReview { get; set; }


    public decimal CandidateAmount { get; set; }  // what candidate paid 
    public decimal PlatformFeeRate { get; set; }
    public decimal PlatformFee { get; set; } 
    public decimal RecruiterEarning { get; set; } 
    public bool IsPaidOut { get; set; }
    public DateTime? PaidOutAt { get; set; }


}
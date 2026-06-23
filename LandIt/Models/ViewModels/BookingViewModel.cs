using Microsoft.AspNetCore.Mvc.Rendering;

namespace LandIt.Models.ViewModels
{
    public class BookingViewModel
    {
        // recruiter info
        public int RecruiterId { get; set; }

        public string? RecruiterName { get; set; }

        public string? RecruiterTitle { get; set; }

        public string? Company { get; set; }

        public decimal HourlyRate { get; set; }


        // selected slot
        public int SelectedTimeSlotId { get; set; }


        // booking details
        public string? JobTitle { get; set; }

        public string? CompanyTarget { get; set; }

        public string? CandidateNotes { get; set; }

        public string? Notes { get; set; }


        // available slots
        public List<SelectListItem>? AvailableSlots { get; set; }
    }
}
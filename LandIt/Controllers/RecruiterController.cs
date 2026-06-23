using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using LandIt.Data;
using LandIt.Models;
using LandIt.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.Controllers
{
    [Authorize(Roles = "Recruiter")]
    public class RecruiterController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<RecruiterController> _logger;

        public RecruiterController(
            ApplicationDbContext db,
            UserManager<AppUser> userManager,
            ILogger<RecruiterController> logger,
            SignInManager<AppUser> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Dashboard(string? search)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var recruiter = await _db.Recruiters
                    .FirstOrDefaultAsync(r => r.UserId == user.Id);

                if (recruiter == null) return RedirectToAction("ApplyAsRecruiter", "Account");

                var query = _db.Recruiters
                    .Where(r => r.Id != recruiter.Id && r.Status == RecruiterStatus.Approved);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower();
                    query = query.Where(r =>
                        r.FullName.ToLower().Contains(s) ||
                        r.Company.ToLower().Contains(s) ||
                        r.Title.ToLower().Contains(s) ||
                        (r.Skills != null && r.Skills.ToLower().Contains(s)))
                        .Include(r => r.Reviews.Where(rv => rv.IsApproved));
                }

                var vm = new RecruiterDashboardViewModel
                {
                    Recruiter = recruiter,
                    TotalBookings = await _db.Bookings.CountAsync(b => b.RecruiterId == recruiter.Id),
                    PendingBookings = await _db.Bookings.CountAsync(b => b.RecruiterId == recruiter.Id && b.Status == BookingStatus.Pending),
                    ActiveSlots = await _db.TimeSlots.CountAsync(t => t.RecruiterId == recruiter.Id && !t.IsBooked && t.IsActive),
                    ReviewsCount = await _db.RecruiterReviews.CountAsync(r => r.RecruiterId == recruiter.Id),
                    AverageRating = await _db.RecruiterReviews.Where(r => r.RecruiterId == recruiter.Id).AverageAsync(r => (double?)r.Rating) ?? 0,
                    UpcomingBookings = await _db.Bookings
                        .Where(b => b.RecruiterId == recruiter.Id &&
                                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) &&
                                    b.TimeSlot.StartTime > DateTime.UtcNow)
                        .Include(b => b.User)
                        .Include(b => b.TimeSlot)
                        .OrderBy(b => b.TimeSlot.StartTime)
                        .Take(3)
                        .ToListAsync(),
                    OtherRecruiters = await query.ToListAsync(),
                    Search = search
                };

                return View(vm);
            }


            catch (Exception ex)
            {
                return Content($"ERROR: {ex.Message} \n\n {ex.StackTrace}");
            }
        }


        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);

            var recruiter = await _db.Recruiters
                .Include(r => r.User)
                .Include(r => r.Availabilities)
                .Include(r => r.Reviews)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return RedirectToAction("Recruiters", "Account");

            var model = new
            {
                recruiter.Id,
                recruiter.FullName,
                recruiter.Company,
                recruiter.Title,
                recruiter.Region,
                recruiter.linkedInURL,
                recruiter.HourlyRate,
                recruiter.Skills,
                recruiter.Status,
                recruiter.CreatedAt,

                AvailabilityCount = recruiter.Availabilities?.Count ?? 0,
                ReviewsCount = recruiter.Reviews?.Count ?? 0,
                AverageRating = recruiter.Reviews != null && recruiter.Reviews.Any()
                    ? recruiter.Reviews.Average(r => r.Rating)
                    : 0
            };

            ViewBag.CanAddTestimonial = await _db.Resumes.AnyAsync(r => r.UserId == recruiter.User.Id)
                         || await _db.QuestionRequests.AnyAsync(q => q.UserId == recruiter.User.Id);

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var recruiter = await _db.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
                return NotFound();

            return View(recruiter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(Recruiter model, string CurrentPassword)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var recruiter = await _db.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(recruiter);

            //  password check
            var check = await _userManager.CheckPasswordAsync(user, CurrentPassword);

            if (!check)
            {
                ModelState.AddModelError("", "Incorrect current password.");
                return View(recruiter);
            }

            // update allowed fields only
            recruiter.FullName = model.FullName;
            recruiter.Company = model.Company;
            recruiter.Title = model.Title;
            recruiter.Region = model.Region;
            recruiter.HourlyRate = model.HourlyRate;
            recruiter.Skills = model.Skills;

            await _db.SaveChangesAsync();

            TempData["ToastMessageSuccess"] = "Profile updated successfully!";

            return RedirectToAction(nameof(Profile));
        }
        
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);

            if (!result.Succeeded)
            {
                TempData["Error"] = "Password change failed.";
                return RedirectToAction("EditProfile");
            }

            TempData["ToastMessageSuccess"] = "Password updated successfully!";
            return RedirectToAction("EditProfile");
        }


        private async Task GenerateTimeSlotsAsync(RecruiterAvailability availability)
        {
            var today = DateTime.UtcNow.Date;

            // Generate slots for next 7 days
            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(i);

                if ((int)date.DayOfWeek != availability.DayOfWeek)
                    continue;

                var currentTime = availability.StartTime;

                while (currentTime.AddHours(1) <= availability.EndTime)
                {
                    var slotStart = date.Add(currentTime.ToTimeSpan());
                    var slotEnd = slotStart.AddHours(1);

                    bool exists = await _db.TimeSlots.AnyAsync(t =>
                        t.RecruiterId == availability.RecruiterId &&
                        t.StartTime == slotStart);

                    if (!exists)
                    {
                        _db.TimeSlots.Add(new TimeSlot
                        {
                            RecruiterId = availability.RecruiterId,
                            RecruiterAvailabilityId = availability.Id,
                            StartTime = slotStart,
                            EndTime = slotEnd,
                            IsBooked = false,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    currentTime = currentTime.AddHours(1);
                }
            }

            await _db.SaveChangesAsync();
        }


        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var recruiter = await _db.Recruiters
                .Include(r => r.Availabilities)
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
                return NotFound();

            return View(recruiter);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(RecruiterAvailability model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var recruiter = await _db.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
                return NotFound();

            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Availability));

            if (model.StartTime >= model.EndTime)
            {
                TempData["Error"] = "End time must be after start time.";
                return RedirectToAction(nameof(Availability));
            }

            var availability = new RecruiterAvailability
            {
                RecruiterId = recruiter.Id,
                DayOfWeek = model.DayOfWeek,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                TimeZone = model.TimeZone,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.RecruiterAvailabilities.Add(availability);
            await _db.SaveChangesAsync();
            await GenerateTimeSlotsAsync(availability);

            TempData["ToastMessageSuccess"] = "Availability added successfully!";
            return RedirectToAction(nameof(Availability));
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAvailability(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var recruiter = await _db.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
                return NotFound();

            var availability = await _db.RecruiterAvailabilities
                .FirstOrDefaultAsync(a => a.Id == id && a.RecruiterId == recruiter.Id);

            if (availability == null)
            {
                TempData["Error"] = "Availability not found.";
                return RedirectToAction(nameof(Availability));
            }

            var bookedSlots = await _db.TimeSlots.AnyAsync(t =>
                                                            t.RecruiterAvailabilityId == availability.Id &&
                                                            t.IsBooked);

            if (bookedSlots)
            {
                TempData["Error"] =
                    "This availability contains booked sessions and cannot be removed.";

                return RedirectToAction(nameof(Availability));
            }

            var slots = await _db.TimeSlots
                .Where(t => t.RecruiterAvailabilityId == availability.Id)
                .ToListAsync();

            // Remove any Bookings that reference these TimeSlots to avoid FK violations
            var slotIds = slots.Select(s => s.Id).ToList();

            var bookings = await _db.Bookings
                .Where(b => slotIds.Contains(b.TimeSlotId))
                .ToListAsync();

            _db.Bookings.RemoveRange(bookings);

            _db.TimeSlots.RemoveRange(slots);

            _db.RecruiterAvailabilities.Remove(availability);

            await _db.SaveChangesAsync();

            TempData["ToastMessageSuccess"] = "Availability removed successfully!";
            return RedirectToAction(nameof(Availability));
        }



        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var recruiter = await _db.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
                return NotFound();

            var bookings = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.TimeSlot)
                .Where(b => b.RecruiterId == recruiter.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBooking(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var recruiter = await _db.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
                return NotFound();

            var booking = await _db.Bookings
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(b => b.Id == id && b.RecruiterId == recruiter.Id);

            if (booking == null)
                return NotFound();

            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;

            if (booking.TimeSlot != null)
                booking.TimeSlot.IsBooked = true;

            await _db.SaveChangesAsync();

            TempData["ToastMessageSuccess"] = "Booking approved!";
            return RedirectToAction(nameof(Bookings));
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var recruiter = await _db.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
                return NotFound();

            var booking = await _db.Bookings
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(b => b.Id == id && b.RecruiterId == recruiter.Id);

            if (booking == null)
                return NotFound();

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            if (booking.TimeSlot != null)
                booking.TimeSlot.IsBooked = false;

            await _db.SaveChangesAsync();

            TempData["ToastMessageSuccess"] = "Booking cancelled.";
            return RedirectToAction(nameof(Bookings));
        }


        [HttpGet]
        public async Task<IActionResult> SessionDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var recruiter = await _db.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return Challenge();

            var booking = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.TimeSlot)
                .Include(b => b.RecruiterReview)
                .FirstOrDefaultAsync(b => b.Id == id && b.RecruiterId == recruiter.Id);

            if (booking == null)
                return NotFound();

            return View(booking);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSession(int id)
        {
            var userId = _userManager.GetUserId(User);

            var recruiter = await _db.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return Challenge();

            var booking = await _db.Bookings
                .FirstOrDefaultAsync(b => b.Id == id && b.RecruiterId == recruiter.Id);

            if (booking == null)
                return NotFound();

            booking.Status = BookingStatus.Completed;
            booking.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["ToastMessageSuccess"] = "Session marked as completed!";
            return RedirectToAction(nameof(SessionDetails), new { id });
        }


        public async Task<IActionResult> Reviews()
        {
            var userId = _userManager.GetUserId(User);

            var recruiter = await _db.Recruiters
                .Include(r => r.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound();

            return View(recruiter.Reviews);
        }

    }
}
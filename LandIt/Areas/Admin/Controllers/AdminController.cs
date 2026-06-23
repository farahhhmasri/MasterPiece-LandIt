using LandIt.Data;
using LandIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var recruiters = await _userManager.GetUsersInRoleAsync("Recruiter");
            var candidates = await _userManager.GetUsersInRoleAsync("Candidate");

            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TotalCandidates = candidates.Count;
            ViewBag.TotalRecruiters = recruiters.Count;
            ViewBag.TotalRecruiterProfiles = await _db.Recruiters.CountAsync();
            ViewBag.PendingRecruiters = await _db.Recruiters.CountAsync(r => r.Status == RecruiterStatus.Pending);
            ViewBag.TotalBookings = await _db.Bookings.CountAsync();
            ViewBag.PendingBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending);
            ViewBag.CompletedBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Completed);
            ViewBag.TotalTestimonials = await _db.Testimonials.CountAsync();
            ViewBag.ApprovedTestimonials = await _db.Testimonials.CountAsync(t => t.IsApproved);
            ViewBag.PendingTestimonials = await _db.Testimonials.CountAsync(t => !t.IsApproved);
            ViewBag.UnreadMessages = await _db.ContactMessages.CountAsync(m => !m.IsRead);
            ViewBag.TotalMessages = await _db.ContactMessages.CountAsync();
            ViewBag.TotalResumes = await _db.Resumes.CountAsync();
            ViewBag.TotalATSResults = await _db.ATSresults.CountAsync();
            ViewBag.PendingPayoutsCount = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Completed && !b.IsPaidOut);
            ViewBag.PendingReviews = await _db.RecruiterReviews.CountAsync(r => !r.IsApproved);
            ViewBag.TotalReviews = await _db.RecruiterReviews.CountAsync();
            ViewBag.ApprovedReviews = await _db.RecruiterReviews.CountAsync(r => r.IsApproved);

            ViewBag.PendingPayoutsAmount = await _db.Bookings
                .Where(b => b.Status == BookingStatus.Completed && !b.IsPaidOut)
                .SumAsync(b => (decimal?)b.RecruiterEarning) ?? 0;

            ViewBag.RecentBookings = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Recruiter)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View();
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }
    }
}

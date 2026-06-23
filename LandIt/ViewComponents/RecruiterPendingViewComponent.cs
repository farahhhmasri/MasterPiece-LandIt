using LandIt.Data;
using LandIt.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.ViewComponents
{
    public class RecruiterPendingViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public RecruiterPendingViewComponent(ApplicationDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return Content("");

            var principal = (System.Security.Claims.ClaimsPrincipal)User;
            if (!principal.IsInRole("Recruiter")) return Content("");

            var userId = _userManager.GetUserId(principal);
            if (string.IsNullOrEmpty(userId)) return Content("");

            var recruiter = await _db.Recruiters.FirstOrDefaultAsync(r => r.UserId == userId);
            if (recruiter == null) return Content("");

            var pending = await _db.Bookings
                .Where(b => b.RecruiterId == recruiter.Id && b.Status == BookingStatus.Pending)
                .Include(b => b.User)
                .Include(b => b.TimeSlot)
                .OrderBy(b => b.TimeSlot!.StartTime)
                .ToListAsync();

            return View(pending);
        }
    }
}

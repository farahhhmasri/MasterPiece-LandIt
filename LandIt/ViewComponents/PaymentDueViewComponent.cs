using LandIt.Data;
using LandIt.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.ViewComponents
{
    public class PaymentDueViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public PaymentDueViewComponent(ApplicationDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return Content("");

            var userId = _userManager.GetUserId((System.Security.Claims.ClaimsPrincipal)User);
            if (string.IsNullOrEmpty(userId)) return Content("");

            var due = await _db.Bookings
                .Where(b => b.UserId == userId
                            && b.Status == BookingStatus.Confirmed
                            && b.PaymentStatus != PaymentStatus.Paid)
                .Include(b => b.Recruiter)
                .Include(b => b.TimeSlot)
                .OrderBy(b => b.TimeSlot!.StartTime)
                .ToListAsync();

            return View(due);
        }
    }
}

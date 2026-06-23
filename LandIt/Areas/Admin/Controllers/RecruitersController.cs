using LandIt.Data;
using LandIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RecruitersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public RecruitersController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? status)
        {
            var q = _db.Recruiters
                .Include(r => r.User)
                .Include(r => r.Reviews)
                .Include(r => r.Bookings)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RecruiterStatus>(status, out var s))
                q = q.Where(r => r.Status == s);

            ViewBag.Filter = status;
            return View(await q.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var r = await _db.Recruiters
                .Include(x => x.User)
                .Include(x => x.Reviews).ThenInclude(rv => rv.User)
                .Include(x => x.Bookings)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return NotFound();
            return View(r);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, RecruiterStatus status)
        {
            var r = await _db.Recruiters.FindAsync(id);
            if (r == null) return NotFound();
            r.Status = status;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Recruiter status set to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id, int recruiterId)
        {
            var rv = await _db.RecruiterReviews.FindAsync(id);
            if (rv == null) return NotFound();
            _db.RecruiterReviews.Remove(rv);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Review deleted.";
            return RedirectToAction(nameof(Details), new { id = recruiterId });
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReview(int id, int recruiterId)
        {
            var review = await _db.RecruiterReviews.FindAsync(id);
            if (review == null)
                return NotFound();

            review.IsApproved = true;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = recruiterId });
        }


    }
}

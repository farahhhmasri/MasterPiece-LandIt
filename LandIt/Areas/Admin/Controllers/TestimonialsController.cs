using LandIt.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TestimonialsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public TestimonialsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? filter)
        {
            var q = _db.Testimonials.Include(t => t.User).AsQueryable();
            if (filter == "approved") q = q.Where(t => t.IsApproved);
            else if (filter == "pending") q = q.Where(t => !t.IsApproved);

            ViewBag.Filter = filter;
            return View(await q.OrderByDescending(t => t.CreatedAt).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t == null) return NotFound();
            t.IsApproved = true;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Testimonial approved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t == null) return NotFound();
            t.IsApproved = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Testimonial rejected.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t == null) return NotFound();
            _db.Testimonials.Remove(t);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Testimonial deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}

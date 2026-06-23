using LandIt.Data;
using LandIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public BookingsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? status, string? filter)
        {
            var q = _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Recruiter)
                .Include(b => b.TimeSlot)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var s))
                q = q.Where(b => b.Status == s);

            if (filter == "unpaid")
                q = q.Where(b => b.Status == BookingStatus.Completed && !b.IsPaidOut);

            ViewBag.Filter = filter;
            ViewBag.Status = status;
            ViewBag.UnpaidCount = await _db.Bookings
                .CountAsync(b => b.Status == BookingStatus.Completed && !b.IsPaidOut);
            ViewBag.UnpaidAmount = await _db.Bookings
                .Where(b => b.Status == BookingStatus.Completed && !b.IsPaidOut)
                .SumAsync(b => (decimal?)b.RecruiterEarning) ?? 0;

            return View(await q.OrderByDescending(b => b.CreatedAt).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            booking.IsPaidOut = true;
            booking.PaidOutAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["ToastMessageSuccess"] = "Booking marked as paid out.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var b = await _db.Bookings.Include(x => x.TimeSlot).FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            b.Status = BookingStatus.Cancelled;
            b.UpdatedAt = DateTime.UtcNow;
            if (b.TimeSlot != null) b.TimeSlot.IsBooked = false;
            await _db.SaveChangesAsync();
            TempData["ToastMessageSuccess"] = "Booking cancelled.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var b = await _db.Bookings
                .Include(x => x.TimeSlot)
                .Include(x => x.Payment)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            if (b.TimeSlot != null) b.TimeSlot.IsBooked = false;
            _db.Bookings.Remove(b);
            await _db.SaveChangesAsync();
            TempData["ToastMessageSuccess"] = "Booking deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
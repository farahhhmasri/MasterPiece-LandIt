using LandIt.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ContactMessagesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ContactMessagesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var msgs = await _db.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync();
            return View(msgs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var m = await _db.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();
            if (!m.IsRead)
            {
                m.IsRead = true;
                await _db.SaveChangesAsync();
            }
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string reply)
        {
            var m = await _db.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();
            m.AdminReply = reply;
            m.RepliedAt = DateTime.UtcNow;
            m.IsRead = true;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Reply saved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();
            _db.ContactMessages.Remove(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Message deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}

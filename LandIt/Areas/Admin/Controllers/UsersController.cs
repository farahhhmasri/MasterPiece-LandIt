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
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public UsersController(ApplicationDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? role, string? q)
        {
            var users = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                users = users.Where(u => u.Email!.Contains(q) || u.FullName.Contains(q) || u.UserName!.Contains(q));
            }

            var list = await users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var rolesMap = new Dictionary<string, IList<string>>();
            foreach (var u in list)
                rolesMap[u.Id] = await _userManager.GetRolesAsync(u);

            if (!string.IsNullOrEmpty(role))
                list = list.Where(u => rolesMap[u.Id].Contains(role)).ToList();

            ViewBag.Roles = rolesMap;
            ViewBag.Filter = role;
            ViewBag.Query = q;
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Promote(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "Recruiter"))
            {
                if (await _userManager.IsInRoleAsync(user, "Candidate"))
                    await _userManager.RemoveFromRoleAsync(user, "Candidate");
                await _userManager.AddToRoleAsync(user, "Recruiter");
                user.IsRecruiter = true;

                if (!await _db.Recruiters.AnyAsync(r => r.UserId == user.Id))
                {
                    _db.Recruiters.Add(new Recruiter
                    {
                        UserId = user.Id,
                        FullName = user.FullName,
                        Company = "—",
                        Title = "—",
                        Region = user.Region,
                        HourlyRate = 0,
                        Skills = "",
                        Status = RecruiterStatus.Pending
                    });
                }
                await _db.SaveChangesAsync();
                TempData["Success"] = $"{user.FullName} promoted to Recruiter.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Demote(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Recruiter"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Recruiter");
                if (!await _userManager.IsInRoleAsync(user, "Candidate"))
                    await _userManager.AddToRoleAsync(user, "Candidate");
                user.IsRecruiter = false;

                var rec = await _db.Recruiters.FirstOrDefaultAsync(r => r.UserId == user.Id);
                if (rec != null) rec.Status = RecruiterStatus.Suspended;

                await _db.SaveChangesAsync();
                TempData["Success"] = $"{user.FullName} demoted to Candidate.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            TempData["Success"] = $"{user.FullName} suspended.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = $"{user.FullName} reactivated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Cannot delete an Admin user.";
                return RedirectToAction(nameof(Index));
            }
            await _userManager.DeleteAsync(user);
            TempData["Success"] = "User deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}

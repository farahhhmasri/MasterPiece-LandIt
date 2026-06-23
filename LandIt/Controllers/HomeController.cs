using DocumentFormat.OpenXml.Spreadsheet;
using LandIt.Data;
using LandIt.Models;
using LandIt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LandIt.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext dbcontext, ILogger<HomeController> logger)
        {
            _dbcontext = dbcontext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Testimonials = await _dbcontext.Testimonials
                .Where(t => t.IsApproved)
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Take(6)
                .ToListAsync();


            // Real statistics from DB
            ViewBag.MockInterviewsDone = await _dbcontext.Bookings
                                               .Where(b => b.Status == BookingStatus.Completed)
                                               .CountAsync();

            ViewBag.VerifiedRecruiters = await _dbcontext.Recruiters
                                                .Where(r => r.Status == RecruiterStatus.Approved && r.User.IsRecruiter == true)
                                                .CountAsync();

            ViewBag.ResumesOptimised = await _dbcontext.ATSresults
                                               .CountAsync();

            ViewBag.SuccessRate = await _dbcontext.Bookings
                                               .Where(b => b.Status == BookingStatus.Completed)
                                               .AverageAsync(b => (double?)b.RecruiterReview.Rating) ?? 0;


            return View();
        }

        public IActionResult Privacy() => View();

        public IActionResult Terms() => View();

        public IActionResult Contact() => View();

        public IActionResult Pricing() => View();



        // (when the user submits the contact form)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactMessage model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                _dbcontext.ContactMessages.Add(model);
                await _dbcontext.SaveChangesAsync();

                TempData["Success"] = "Your message has been sent! We'll get back to you soon.";
                return RedirectToAction(nameof(Contact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save contact message");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
                return View(model);
            }
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel
        {
            RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
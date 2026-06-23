using LandIt.Data;
using LandIt.Models;
using LandIt.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.Controllers;

[Authorize]
public class TestimonialController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<TestimonialController> _logger;

    public TestimonialController(
        ApplicationDbContext db,
        UserManager<AppUser> userManager,
        ILogger<TestimonialController> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }


    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Check user has used at least one feature
        var hasResume = await _db.Resumes.AnyAsync(r => r.UserId == user.Id);
        var hasQuestions = await _db.QuestionRequests.AnyAsync(q => q.UserId == user.Id);

        if (!hasResume && !hasQuestions)
        {
            TempData["Error"] = "You need to use the ATS Analyzer or Question Generator before leaving a testimonial.";
            return RedirectToAction("Profile", "Account");
        }

        // Pre-select source based on what they've used
        var defaultSource = hasResume
            ? TestimonialSource.ATSAnalyzer
            : TestimonialSource.QuestionGenerator;

        return View(new TestimonialViewModel { Source = defaultSource });
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TestimonialViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Prevent duplicate testimonials for the same source
        var alreadyExists = await _db.Testimonials
            .AnyAsync(t => t.UserId == user.Id && t.Source == model.Source);

        if (alreadyExists)
        {
            ModelState.AddModelError(string.Empty,
                $"You have already submitted a testimonial for {model.Source.ToString().Replace("_", " ")}.");
            return View(model);
        }

        try
        {
            var testimonial = new Testimonial
            {
                UserId = user.Id,
                Content = model.Content,
                Source = model.Source,
                Rating = model.Rating,
                IsApproved = false,   // admin must approve before it shows on landing page
                CreatedAt = DateTime.UtcNow
            };

            _db.Testimonials.Add(testimonial);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Thank you for your testimonial! It will appear on the site after review.";
            return RedirectToAction(nameof(My));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save testimonial");
            ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
            return View(model);
        }
    }

    public async Task<IActionResult> My()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var testimonials = await _db.Testimonials
            .Where(t => t.UserId == user.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View(testimonials);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

        if (testimonial == null) return NotFound();

        // Only allow deletion if not yet approved
        if (testimonial.IsApproved)
        {
            TempData["Error"] = "You cannot delete an approved testimonial. Contact support if needed.";
            return RedirectToAction(nameof(My));
        }

        _db.Testimonials.Remove(testimonial);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Testimonial deleted.";
        return RedirectToAction(nameof(My));
    }
}
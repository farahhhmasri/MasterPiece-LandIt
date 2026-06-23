using LandIt.Data;
using LandIt.Models;
using LandIt.Models.ViewModels;
using LandIt.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.Controllers;

[Authorize]
public class QuestionsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly GroqService _groq;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(
        ApplicationDbContext db,
        UserManager<AppUser> userManager,
        GroqService groq,
        ILogger<QuestionsController> logger)
    {
        _db = db;
        _userManager = userManager;
        _groq = groq;
        _logger = logger;
    }

    // show the form
    public IActionResult Index() => View(new QuestionRequestViewModel());

    // call Groq, save to DB, show results
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(QuestionRequestViewModel model)
    {
        if (!ModelState.IsValid) return View("Index", model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        try
        {
            // 1. Call Groq
            var questions = await _groq.GenerateQuestionsAsync(
                model.JobTitle,
                model.JobDescription,
                model.Level,
                model.CompanyName);

            // 2. Save QuestionRequest
            var request = new QuestionRequest
            {
                UserId = user.Id,
                JobTitle = model.JobTitle,
                JobDescription = model.JobDescription,
                level = model.Level,
                CreatedAt = DateTime.UtcNow
            };
            _db.QuestionRequests.Add(request);
            await _db.SaveChangesAsync();

            // 3. Save generated questions
            var generatedQuestions = questions.Select(q => new GeneratedQuestion
            {
                QuestionRequestId = request.Id,
                QuestionText = q.QuestionText,
                Category = q.Category,
                Tip = q.Tip
            }).ToList();

            _db.GeneratedQuestions.AddRange(generatedQuestions);
            await _db.SaveChangesAsync();

            // 4. Pass to result view
            return View("Result", generatedQuestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Question generation failed");
            ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
            return View("Index", model);
        }
    }

    // past question requests
    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var requests = await _db.QuestionRequests
            .Where(q => q.UserId == user.Id)
            .Include(q => q.GeneratedQuestions)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    // view a past request's questions
    public async Task<IActionResult> View(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var request = await _db.QuestionRequests
            .Include(q => q.GeneratedQuestions)
            .FirstOrDefaultAsync(q => q.Id == id && q.UserId == user.Id);

        if (request == null) return NotFound();

        return View("Result", request.GeneratedQuestions.ToList());
    }
}
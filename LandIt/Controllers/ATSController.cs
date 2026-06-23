using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using LandIt.Data;
using LandIt.Models;
using LandIt.Models.ViewModels;
using LandIt.Services;

namespace LandIt.Controllers;

[Authorize]
public class ATSController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly ResumeExtractorService _extractor;
    private readonly GroqService _groq;
    private readonly ResumePdfBuilder _pdfBuilder;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ATSController> _logger;

    public ATSController(
        ApplicationDbContext db,
        UserManager<AppUser> userManager,
        ResumeExtractorService extractor,
        GroqService groq,
        ResumePdfBuilder pdfBuilder,
        IWebHostEnvironment env,
        ILogger<ATSController> logger)
    {
        _db = db;
        _userManager = userManager;
        _extractor = extractor;
        _groq = groq;
        _pdfBuilder = pdfBuilder;
        _env = env;
        _logger = logger;
    }

    //  upload form
    public IActionResult Index() => View(new ATSUploadViewModel());


    // analyze resume, save data, call Groq, show result page with score/suggestions
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(ATSUploadViewModel model)
    {
        if (!ModelState.IsValid) return View("Index", model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        try
        {
            // 1. Validate file
            var ext = Path.GetExtension(model.ResumeFile.FileName).ToLowerInvariant();
            if (ext is not ".pdf" and not ".docx")
            {
                ModelState.AddModelError("ResumeFile", "Only PDF and DOCX files are accepted.");
                return View("Index", model);
            }

            if (model.ResumeFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ResumeFile", "File must be smaller than 5MB.");
                return View("Index", model);
            }

            // 2. Save file to wwwroot/resumes/
            var uploadsDir = Path.Combine(_env.WebRootPath, "resumes");
            Directory.CreateDirectory(uploadsDir);
            var uniqueName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, uniqueName);
            await using (var fs = System.IO.File.Create(filePath))
                await model.ResumeFile.CopyToAsync(fs);

            // 3. Extract text
            var parsedText = await _extractor.ExtractTextAsync(model.ResumeFile);

            // 4. Save Resume record
            var resume = new Resume
            {
                UserId = user.Id,
                FilePath = $"/resumes/{uniqueName}",
                ParsedText = parsedText
            };
            _db.Resumes.Add(resume);
            await _db.SaveChangesAsync();

            // 5. Call Groq for ATS analysis
            var analysis = await _groq.AnalyzeAsync(
                parsedText,
                model.JobTitle,
                model.JobDescription);

            // 6. Save ATSResult
            var atsResult = new ATSresult
            {
                ResumeId = resume.Id,
                JobTitle = model.JobTitle,
                Score = analysis.Score,
                Suggestions = JsonSerializer.Serialize(analysis.Suggestions),
                CreatedAt = DateTime.UtcNow
            };
            _db.ATSresults.Add(atsResult);
            await _db.SaveChangesAsync();

            // 7. Build result ViewModel and show result page
            var vm = new ATSResultViewModel
            {
                ResumeId = resume.Id,
                ATSResultId = atsResult.Id,
                Score = analysis.Score,
                MatchedKeywords = analysis.MatchedKeywords,
                MissingKeywords = analysis.MissingKeywords,
                Suggestions = analysis.Suggestions,
                JobTitle = model.JobTitle
            };

            // Store in TempData to survive the redirect
            TempData["ATSResult"] = JsonSerializer.Serialize(vm);
            TempData["ResumeText"] = parsedText;
            TempData["JobTitle"] = model.JobTitle;
            TempData["CompanyName"] = model.CompanyName;
            TempData["JobDescription"] = model.JobDescription;
            TempData["ExperienceLevel"] = model.ExperienceLevel;
            TempData["ResumeId"] = resume.Id;
            TempData["ATSResultId"] = atsResult.Id;

            return RedirectToAction(nameof(Result));
        }
        catch (NotSupportedException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ATS analysis failed");
            ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
            return View("Index", model);
        }
    }


    //shows score, keywords, suggestions 
    public IActionResult Result()
    {
        var json = TempData["ATSResult"] as string;
        if (string.IsNullOrEmpty(json)) return RedirectToAction(nameof(Index));

        TempData.Keep();

        var vm = JsonSerializer.Deserialize<ATSResultViewModel>(json);
        return View(vm);
    }


    //rewrite resume using Groq 
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateResume()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Read from TempData
        var resumeText = TempData["ResumeText"] as string;
        var jobTitle = TempData["JobTitle"] as string;
        var companyName = TempData["CompanyName"] as string ?? "";
        var jobDescription = TempData["JobDescription"] as string;
        var expLevel = TempData["ExperienceLevel"] as string ?? "mid";
        var resumeId = TempData["ResumeId"] != null ? Convert.ToInt32(TempData["ResumeId"]) : 0;
        var atsResultId = TempData["ATSResultId"] != null ? Convert.ToInt32(TempData["ATSResultId"]) : 0;


        // Keep TempData alive for the result page
        TempData.Keep();

        if (string.IsNullOrEmpty(resumeText) || string.IsNullOrEmpty(jobTitle))
            return RedirectToAction(nameof(Index));

        try
        {
            // 1. Call Groq to rewrite resume
            var parsed = await _groq.RewriteResumeAsync(
                resumeText, jobTitle, companyName, jobDescription ?? "", expLevel);

            // 2. Generate PDF
            //var (fileUrl, fileName) = await _pdfBuilder.GenerateAsync(parsed);
            (string fileUrl, string fileName) = await _pdfBuilder.GenerateAsync(parsed);

            // 3. Save GeneratedResume to DB
            var generated = new GeneratedResume
            {
                UserId = user.Id,
                ATSResultId = atsResultId > 0 ? atsResultId : null,
                JobTitle = jobTitle,
                Content = JsonSerializer.Serialize(parsed),
                FileUrl = fileUrl,
                FileName = fileName,
                AIModelUsed = "llama-3.3-70b-versatile",
                CreatedAt = DateTime.UtcNow
            };
            _db.GeneratedResumes.Add(generated);
            await _db.SaveChangesAsync();

            // 4. Update result ViewModel with download link
            var json = TempData["ATSResult"] as string;
            if (!string.IsNullOrEmpty(json))
            {
                var vm = JsonSerializer.Deserialize<ATSResultViewModel>(json)!;
                vm.GeneratedFileUrl = fileUrl;
                TempData["ATSResult"] = JsonSerializer.Serialize(vm);
            }

            TempData["Success"] = "Your ATS-optimised resume is ready!";
            return RedirectToAction(nameof(Result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resume generation failed");
            TempData["Error"] = "Resume generation failed. Please try again.";
            return RedirectToAction(nameof(Result));
        }
    }


    // Download?file=xyz.pdf 
    public IActionResult Download(string file)
    {
        if (string.IsNullOrWhiteSpace(file) || file.Contains("..") || !file.EndsWith(".pdf"))
            return BadRequest("Invalid file.");

        var path = Path.Combine(_env.WebRootPath, "generated", file);
        if (!System.IO.File.Exists(path)) return NotFound("File not found or expired.");

        return PhysicalFile(path, "application/pdf", file);
    }


    //users past analyses
    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var resumes = await _db.Resumes
            .Where(r => r.UserId == user.Id)
            .Include(r => r.ATSResults)
            .OrderByDescending(r => r.Id)
            .ToListAsync();

        return View(resumes);
    }
}
using LandIt.Data;
using LandIt.Models;
using LandIt.Models.ViewModels;
using LandIt.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandIt.Controllers
{
    
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly NavigationService _nav;




        public AccountController(ILogger<AccountController> logger, SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment, NavigationService nav)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _environment = environment;
            _nav = nav;
        }


        [Authorize]
        public async Task<IActionResult> DashboardRouter()
        {
            var user = await _userManager.GetUserAsync(User);
            var url = await _nav.GetHomeRouteAsync(user);
            return Redirect(url);
        }

        [Authorize]
        public async Task<IActionResult> ProfileRouter()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Index", "Home");

            var route = await _nav.GetProfileRouteAsync(user);

            return Redirect(route);
        }


        public IActionResult Login(string? returnUrl = null)
        {
            return Redirect($"/Identity/Account/Login?returnUrl={returnUrl}"); // so the user returns to the page they were trying to access after logging in
        }

        public IActionResult Register(string? returnUrl = null)
        {
            return Redirect($"/Identity/Account/Register?returnUrl={returnUrl}");
        }


        [Authorize(Roles ="Candidate")]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Stats from DB
            ViewBag.BookingsCount = await _context.Bookings.CountAsync(b => b.UserId == user.Id);
            ViewBag.ResumesCount = await _context.Resumes.CountAsync(r => r.UserId == user.Id);
            ViewBag.QuestionsCount = await _context.QuestionRequests.CountAsync(q => q.UserId == user.Id);

            // Upcoming bookings — confirmed or pending, future slots only
            ViewBag.UpcomingBookings = await _context.Bookings
                .Where(b => b.UserId == user.Id &&
                            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) &&
                            b.TimeSlot.StartTime > DateTime.UtcNow)
                .Include(b => b.Recruiter)
                .Include(b => b.TimeSlot)
                .OrderBy(b => b.TimeSlot.StartTime)
                .Take(3)
                .ToListAsync();

            // Recent resumes with ATS results
            ViewBag.RecentResumes = await _context.Resumes
                .Where(r => r.UserId == user.Id)
                .Include(r => r.ATSResults)
                .OrderByDescending(r => r.Id)
                .Take(3)
                .ToListAsync();

            ViewBag.CanAddTestimonial = await _context.Resumes.AnyAsync(r => r.UserId == user.Id)
                         || await _context.QuestionRequests.AnyAsync(q => q.UserId == user.Id);

            return View(user);
        }


        private void ClearPasswords(EditProfileViewModel model)
        {
            model.CurrentPassword = null;
            model.NewPassword = null;
            model.ConfirmPassword = null;
        }

        [Authorize(Roles = "Candidate")]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Region = user.Region,
                PhoneNumber = user.PhoneNumber,
                ExistingPhotoPath = user.Photo
            };

            ClearPasswords(model);

            return View(model);
        }

        [Authorize(Roles = "Candidate")]
        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            // Password change is optional
            // Remove validation if NewPassword is empty
            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.Remove(nameof(model.NewPassword));
                ModelState.Remove(nameof(model.ConfirmPassword));
            }

            if (!ModelState.IsValid)
            {
                ClearPasswords(model);
                return View(model);
            }

            // VERIFY CURRENT PASSWORD
            var passwordValid = await _userManager.CheckPasswordAsync(
                user,
                model.CurrentPassword
            );

            if (!passwordValid)
            {
                ModelState.AddModelError(
                    "CurrentPassword",
                    "Incorrect password."
                );

                ClearPasswords(model);
                return View(model);
            }

            // KEEP CURRENT IMAGE
            string imagePath = user.Photo ?? "/images/default-profile.png";

            // IMAGE UPLOAD
            if (model.Photo != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                string extension = Path.GetExtension(model.Photo.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "Photo",
                        "Only JPG, JPEG, and PNG files are allowed."
                    );

                    ClearPasswords(model);
                    return View(model);
                }

                if (model.Photo.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "Photo",
                        "File size cannot exceed 2MB."
                    );

                    ClearPasswords(model);
                    return View(model);
                }

                string uploadsFolder = Path.Combine(
                    _environment.WebRootPath,
                    "images/users"
                );

                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName =
                    Guid.NewGuid().ToString() + extension;

                string filePath = Path.Combine(
                    uploadsFolder,
                    uniqueFileName
                );

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Photo.CopyToAsync(stream);
                }

                imagePath = "/images/users/" + uniqueFileName;
            }

            // UPDATE USER INFO
            user.FullName = model.FullName;
            user.Region = model.Region;
            user.PhoneNumber = model.PhoneNumber;
            user.Photo = imagePath;

            // OPTIONAL PASSWORD CHANGE
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError(
                        "ConfirmPassword",
                        "Passwords do not match."
                    );

                    ClearPasswords(model);
                    return View(model);
                }

                var passwordResult = await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword,
                    model.NewPassword
                );

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description
                        );
                    }

                    ClearPasswords(model);
                    return View(model);
                }
            }

            // SAVE USER
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                ClearPasswords(model);
                return View(model);
            }

            TempData["ToastMessageSuccess"] =
                "Your profile was updated successfully!";

            return RedirectToAction("Profile");
        }



        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Dashboard(string? search, Region? region)
        {
            var user = await _userManager.GetUserAsync(User);

            var query = _context.Recruiters
                .AsQueryable()
                .Where(r => r.Status == RecruiterStatus.Approved);

            if (region != null)
                query = query.Where(r => r.Region == region);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                query = query.Where(r =>
                    r.FullName.ToLower().Contains(search) ||
                    r.Company.ToLower().Contains(search) ||
                    r.Title.ToLower().Contains(search) ||
                    r.Skills.ToLower().Contains(search));
            }

            var model = new DashboardViewModel
            {
                Search = search,
                RegionFilter = region,

                Recruiters = await query
                    .Include(r => r.Reviews.Where(rv => rv.IsApproved))
                    .ToListAsync(),

                UpcomingBookings = await _context.Bookings
                    .Where(b => b.UserId == user.Id &&
                                (b.Status == BookingStatus.Pending ||
                                 b.Status == BookingStatus.Confirmed) &&
                                b.TimeSlot.StartTime > DateTime.UtcNow)
                    .Include(b => b.Recruiter)
                    .Include(b => b.TimeSlot)
                    .OrderBy(b => b.TimeSlot.StartTime)
                    .Take(3)
                    .ToListAsync()
            };

            return View(model);
        }



        [Authorize]
        public async Task<IActionResult> MyResumes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var resumes = await _context.Resumes
                .Where(r => r.UserId == user.Id)
                .Include(r => r.ATSResults)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return View(resumes);
        }


        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Unauthorized();

            user.Bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .Include(b => b.Recruiter)
                .Include(b => b.TimeSlot)
                .Include(b => b.Payment)
                .Include(b => b.RecruiterReview)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(user);
        }


        [Authorize]
        public async Task<IActionResult> MyQuestions()
        {
            var userId = _userManager.GetUserId(User);

            var user = await _userManager.Users
                .Include(u => u.QuestionRequests)
                    .ThenInclude(q => q.GeneratedQuestions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Unauthorized();

            return View(user);
        }

        public async Task<IActionResult> Recruiters()
        {
            bool alreadyApplied = false;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    alreadyApplied = await _context.Recruiters
                        .AnyAsync(r => r.UserId == user.Id);
                }
            }

            ViewBag.AlreadyApplied = alreadyApplied;

            return View();
        }


        [Authorize(Roles ="Candidate")]
        [HttpPost]
        public async Task<IActionResult> ApplyRecruiter(Recruiter model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            bool exists = _context.Recruiters.Any(r => r.UserId == user.Id);

            if (exists)
            {
                TempData["Error"] = "You already applied.";
                return RedirectToAction("Recruiters");
            }

            var recruiter = new Recruiter
            {
                UserId = user.Id,

                FullName = user.FullName,
                Region = user.Region,

                Company = model.Company,
                Title = model.Title,
                HourlyRate = model.HourlyRate,
                linkedInURL = model.linkedInURL,
                Skills = model.Skills,

                Status = RecruiterStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Recruiters.Add(recruiter);
            await _context.SaveChangesAsync();

            TempData["ToastMessageSuccess"] = "Application submitted successfully!";
            return RedirectToAction("Recruiters");
        }

    }
}

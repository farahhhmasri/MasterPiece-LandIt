using LandIt.Data;
using LandIt.Models;
using LandIt.Models.ViewModels;
using LandIt.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LandIt.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly StripeService _stripe;

        public BookingsController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            StripeService stripe)
        {
            _context = context;
            _userManager = userManager;
            _stripe = stripe;
        }

        // Create Booking - Show Form
        [HttpGet]
        public async Task<IActionResult> Create(int recruiterId)
        {
            var recruiter = await _context.Recruiters
                .Include(r => r.TimeSlots)
                .FirstOrDefaultAsync(r => r.Id == recruiterId);

            if (recruiter == null)
                return NotFound();

            var availableSlots = recruiter.TimeSlots
                .Where(t =>
                    !t.IsBooked &&
                    t.IsActive &&
                    t.StartTime > DateTime.UtcNow)
                .OrderBy(t => t.StartTime)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = $"{t.StartTime:dd MMM yyyy | hh:mm tt} - {t.EndTime:hh:mm tt}"
                })
                .ToList();

            var model = new BookingViewModel
            {
                RecruiterId = recruiter.Id,
                RecruiterName = recruiter.FullName,
                RecruiterTitle = recruiter.Title,
                Company = recruiter.Company,
                HourlyRate = recruiter.HourlyRate,
                AvailableSlots = availableSlots
            };

            return View(model);
        }


        // Create Booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .Include(r => r.TimeSlots)
                .FirstOrDefaultAsync(r => r.Id == model.RecruiterId);

            if (recruiter == null)
                return NotFound();

            var selectedSlot = await _context.TimeSlots
                .FirstOrDefaultAsync(t =>
                    t.Id == model.SelectedTimeSlotId &&
                    !t.IsBooked &&
                    t.IsActive);

            if (selectedSlot == null)
            {
                ModelState.AddModelError("", "Selected slot is unavailable.");
            }

            if (!ModelState.IsValid)
            {
                model.RecruiterName = recruiter.FullName;
                model.RecruiterTitle = recruiter.Title;
                model.Company = recruiter.Company;
                model.HourlyRate = recruiter.HourlyRate;
                model.AvailableSlots = recruiter.TimeSlots
                    .Where(t =>
                        !t.IsBooked &&
                        t.IsActive &&
                        t.StartTime > DateTime.UtcNow)
                    .OrderBy(t => t.StartTime)
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = $"{t.StartTime:dd MMM yyyy | hh:mm tt} - {t.EndTime:hh:mm tt}"
                    })
                    .ToList();

                return View(model);
            }

            var booking = new Booking
            {
                UserId = user.Id,
                RecruiterId = recruiter.Id,
                TimeSlotId = selectedSlot.Id,

                JobTitle = model.JobTitle,
                CompanyTarget = model.CompanyTarget,
                CandidateNotes = model.CandidateNotes,
                Notes = model.Notes,

                Status = BookingStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            selectedSlot.IsBooked = true;

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            TempData["ToastMessageSuccess"] =
                "Your session has been booked successfully.";

            return RedirectToAction(nameof(MyBookings));
        }


        // List Bookings
        public async Task<IActionResult> MyBookings()
        {
            return RedirectToAction("MyBookings", "Account");
        }

        // Cancel Booking
        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(b =>
                    b.Id == id &&
                    b.UserId == user.Id);

            if (booking == null)
                return NotFound();

            booking.Status = BookingStatus.Cancelled;

            if (booking.TimeSlot != null)
            {
                booking.TimeSlot.IsBooked = false;
            }

            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["ToastMessageSuccess"] =
                "Booking cancelled successfully.";

            return RedirectToAction(nameof(MyBookings));
        }


        // Pay for a confirmed booking via Stripe Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Recruiter)
                .Include(b => b.TimeSlot)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Confirmed)
            {
                TempData["ToastMessageSuccess"] = "Booking must be confirmed by the recruiter before payment.";
                return RedirectToAction("MyBookings", "Account");
            }

            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                TempData["ToastMessageSuccess"] = "Booking is already paid.";
                return RedirectToAction("MyBookings", "Account");
            }

            var amount = booking.Recruiter?.HourlyRate ?? 0m;
            if (amount <= 0)
            {
                TempData["ToastMessageSuccess"] = "Recruiter rate is not set.";
                return RedirectToAction("MyBookings", "Account");
            }

            var successUrl = Url.Action(nameof(PaymentSuccess), "Bookings", null, Request.Scheme)!;
            var cancelUrl = Url.Action(nameof(PaymentCancel), "Bookings", new { id = booking.Id }, Request.Scheme)!;

            var session = await _stripe.CreateCheckoutSessionAsync(
                bookingId: booking.Id,
                productName: $"Session with {booking.Recruiter!.FullName}",
                description: $"{booking.TimeSlot!.StartTime:dd MMM yyyy HH:mm} — {booking.Recruiter.Title} at {booking.Recruiter.Company}",
                amount: amount,
                customerEmail: user.Email!,
                successUrl: successUrl,
                cancelUrl: cancelUrl
            );

            var payment = booking.Payment ?? new Payment
            {
                BookingId = booking.Id,
                Provider = "Stripe",
                Amount = amount,
                Status = PaymentStatus.Pending
            };
            payment.StripeSessionId = session.Id;
            payment.Amount = amount;
            payment.Provider = "Stripe";
            if (booking.Payment == null) _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            return Redirect(session.Url);
        }


        // Stripe success redirect
        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(string session_id)
        {
            if (string.IsNullOrWhiteSpace(session_id))
                return RedirectToAction("MyBookings", "Account");

            var session = await _stripe.RetrieveSessionAsync(session_id);

            if (!int.TryParse(session.ClientReferenceId, out var bookingId))
                return RedirectToAction("MyBookings", "Account");

            var booking = await _context.Bookings
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            if (string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                booking.PaymentStatus = PaymentStatus.Paid;
                booking.UpdatedAt = DateTime.UtcNow;

                if (booking.Payment != null)
                {
                    booking.Payment.Status = PaymentStatus.Paid;
                    booking.Payment.StripePaymentIntentId = session.PaymentIntentId;
                    booking.Payment.PaidAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                TempData["ToastMessageSuccess"] = "Payment received — your session is confirmed.";
            }
            else
            {
                TempData["ToastMessageSuccess"] = "Payment is pending. We'll update your booking once it clears.";
            }

            return RedirectToAction("MyBookings", "Account");
        }


        // Stripe cancel redirect
        [HttpGet]
        public IActionResult PaymentCancel(int id)
        {
            TempData["ToastMessageSuccess"] = "Payment cancelled.";
            return RedirectToAction("MyBookings", "Account");
        }


        // Candidate submits a review for a completed booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int bookingId, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (rating < 1 || rating > 5)
            {
                TempData["ToastMessageSuccess"] = "Rating must be between 1 and 5 stars.";
                return RedirectToAction("MyBookings", "Account");
            }

            var booking = await _context.Bookings
                .Include(b => b.RecruiterReview)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == user.Id);

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Completed)
            {
                TempData["ToastMessageSuccess"] = "You can only review completed sessions.";
                return RedirectToAction("MyBookings", "Account");
            }

            if (booking.RecruiterReview != null)
            {
                TempData["ToastMessageSuccess"] = "You've already reviewed this session.";
                return RedirectToAction("MyBookings", "Account");
            }

            var review = new RecruiterReview
            {
                UserId = user.Id,
                RecruiterId = booking.RecruiterId,
                BookingId = booking.Id,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.RecruiterReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["ToastMessageSuccess"] = "Thanks for your review!";
            return RedirectToAction("MyBookings", "Account");
        }



        [HttpGet]
        public async Task<IActionResult> SessionDetail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Recruiter)
                .Include(b => b.TimeSlot)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id &&
                    (b.UserId == user.Id || b.Recruiter.UserId == user.Id));

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Confirmed || booking.PaymentStatus != PaymentStatus.Paid)
            {
                TempData["ToastMessageError"] = "Session is not ready yet. It must be confirmed and paid.";
                return RedirectToAction("MyBookings", "Account");
            }

            // Generate a unique room URL — no API needed
            if (string.IsNullOrEmpty(booking.MeetingUrl ))
            {
                booking.MeetingUrl = $"https://whereby.com/landit-session-{booking.Id}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                await _context.SaveChangesAsync();
            }

            return View(booking);
        }


    }
}
using LandIt.Data;
using LandIt.Models;
using LandIt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace LandIt.Controllers
{
    [Route("api/stripe")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly StripeService _stripe;
        private readonly ILogger<StripeWebhookController> _log;

        public StripeWebhookController(ApplicationDbContext db, StripeService stripe, ILogger<StripeWebhookController> log)
        {
            _db = db;
            _stripe = stripe;
            _log = log;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            Event stripeEvent;
            try
            {
                if (!string.IsNullOrWhiteSpace(_stripe.WebhookSecret))
                {
                    stripeEvent = EventUtility.ConstructEvent(
                        json,
                        Request.Headers["Stripe-Signature"],
                        _stripe.WebhookSecret);
                }
                else
                {
                    stripeEvent = EventUtility.ParseEvent(json);
                }
            }
            catch (StripeException e)
            {
                _log.LogWarning(e, "Stripe webhook signature verification failed.");
                return BadRequest();
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null && int.TryParse(session.ClientReferenceId, out var bookingId))
                {
                    var booking = await _db.Bookings
                        .Include(b => b.Payment)
                        .FirstOrDefaultAsync(b => b.Id == bookingId);

                   
                    if (booking != null && string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                    {
                        booking.PaymentStatus = PaymentStatus.Paid;
                        booking.UpdatedAt = DateTime.UtcNow;

                        // Payout calculation
                        const decimal feeRate = 0.10m;
                        decimal amountPaid = session.AmountTotal.HasValue
                            ? session.AmountTotal.Value / 100m   // Stripe amounts are in cents
                            : 0m;

                        booking.CandidateAmount = amountPaid;
                        booking.PlatformFeeRate = feeRate;
                        booking.PlatformFee = Math.Round(amountPaid * feeRate, 2);
                        booking.RecruiterEarning = amountPaid - booking.PlatformFee;
                        booking.IsPaidOut = false;

                        if (booking.Payment != null)
                        {
                            booking.Payment.Status = PaymentStatus.Paid;
                            booking.Payment.StripePaymentIntentId = session.PaymentIntentId;
                            booking.Payment.PaidAt = DateTime.UtcNow;
                        }

                        await _db.SaveChangesAsync();
                    }
                }
            }

            return Ok();
        }
    }
}

using Stripe;
using Stripe.Checkout;

namespace LandIt.Services
{
    public class StripeService
    {
        private readonly IConfiguration _config;

        public StripeService(IConfiguration config)
        {
            _config = config;
            StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        }

        public string Currency => (_config["Stripe:Currency"] ?? "usd").ToLowerInvariant();
        public string WebhookSecret => _config["Stripe:WebhookSecret"] ?? "";

        public async Task<Session> CreateCheckoutSessionAsync(
            int bookingId,
            string productName,
            string description,
            decimal amount,
            string customerEmail,
            string successUrl,
            string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                PaymentMethodTypes = new List<string> { "card" },
                CustomerEmail = customerEmail,
                ClientReferenceId = bookingId.ToString(),
                Metadata = new Dictionary<string, string> { ["bookingId"] = bookingId.ToString() },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = Currency,
                            UnitAmount = (long)(amount * 100m),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = productName,
                                Description = description
                            }
                        }
                    }
                },
                SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl
            };

            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        public async Task<Session> RetrieveSessionAsync(string sessionId)
        {
            var service = new SessionService();
            return await service.GetAsync(sessionId);
        }
    }
}

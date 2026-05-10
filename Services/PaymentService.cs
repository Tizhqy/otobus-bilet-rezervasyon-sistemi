using System.Security.Cryptography;
using System.Text;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class PaymentService : IPaymentService
    {
        /// <summary>
        /// Validates credit card information including format, expiry, and Luhn check.
        /// </summary>
        public bool ValidateCard(string cardNumber, string expiryDate, string cvv)
        {
            cardNumber = cardNumber?.Replace(" ", "").Replace("-", "") ?? "";

            // Card number validation (16 digits)
            if (cardNumber.Length != 16 || !cardNumber.All(char.IsDigit))
                return false;

            // CVV validation (3-4 digits)
            if (string.IsNullOrWhiteSpace(cvv) || cvv.Length < 3 || !cvv.All(char.IsDigit))
                return false;

            // Expiry date validation (MM/yy format)
            if (!DateTime.TryParseExact(expiryDate, "MM/yy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var expiryDateTime))
                return false;

            // Check if card is expired
            var lastDayOfMonth = new DateTime(expiryDateTime.Year, expiryDateTime.Month,
                DateTime.DaysInMonth(expiryDateTime.Year, expiryDateTime.Month));

            if (lastDayOfMonth < DateTime.Today)
                return false;

            // Luhn algorithm validation
            return ValidateLuhn(cardNumber);
        }

        /// <summary>
        /// Generates a unique payment reference number.
        /// </summary>
        public string GenerateReferenceNumber()
        {
            return Guid.NewGuid().ToString("N")[..12].ToUpper();
        }

        public string GenerateReferenceNumber(int ticketId, string idempotencyKey)
        {
            if (ticketId <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticketId), "Ticket id must be positive.");

            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("Idempotency key must be provided.", nameof(idempotencyKey));

            var normalizedKey = idempotencyKey.Trim().ToLowerInvariant();
            var payload = $"{ticketId}:{normalizedKey}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash)[..12];
        }

        /// <summary>
        /// Checks if the payment timeout has expired.
        /// </summary>
        public bool IsPaymentExpired(DateTime createdAt, int timeoutMinutes)
        {
            return createdAt.AddMinutes(timeoutMinutes) < DateTime.UtcNow;
        }

        /// <summary>
        /// Validates card number using Luhn algorithm.
        /// </summary>
        private static bool ValidateLuhn(string number)
        {
            int sum = 0;
            bool alternate = false;

            for (int i = number.Length - 1; i >= 0; i--)
            {
                int digit = number[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }
    }
}

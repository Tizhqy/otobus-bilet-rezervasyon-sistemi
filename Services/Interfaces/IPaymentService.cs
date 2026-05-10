namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface IPaymentService
    {
        /// <summary>
        /// Validates credit card information including format, expiry, and Luhn check.
        /// </summary>
        bool ValidateCard(string cardNumber, string expiryDate, string cvv);

        /// <summary>
        /// Generates a unique payment reference number.
        /// </summary>
        string GenerateReferenceNumber();

        /// <summary>
        /// Generates a deterministic reference number for idempotent payment completion.
        /// </summary>
        string GenerateReferenceNumber(int ticketId, string idempotencyKey);

        /// <summary>
        /// Checks if the payment timeout has expired.
        /// </summary>
        bool IsPaymentExpired(DateTime createdAt, int timeoutMinutes);
    }
}

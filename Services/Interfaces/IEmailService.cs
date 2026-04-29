using OtobusBiletRezervasyon.DTOs.Ticket;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName);
        Task<bool> SendTicketConfirmationEmailAsync(string toEmail, string firstName, TicketResponseDto ticket, string referenceNo);
    }
}

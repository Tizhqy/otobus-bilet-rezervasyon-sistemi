namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
    }
}

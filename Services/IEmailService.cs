using System.Threading.Tasks;

namespace CloudStorage.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken);
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}

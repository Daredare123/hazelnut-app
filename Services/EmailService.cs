using System.Threading.Tasks;

namespace HazelnutVeb.Services
{
    public class EmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Dummy implementation satisfying compiler requirements without modifying authentication/business logic
            System.Console.WriteLine($"Email sent to {toEmail}: {subject}");
            return Task.CompletedTask;
        }
    }
}

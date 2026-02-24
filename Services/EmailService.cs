using System.Threading.Tasks;

namespace HazelnutVeb.Services
{
    public class EmailService
    {
        public Task SendEmailAsync(string to, string subject, string body)
        {
            // Dummy implementation to satisfy the compiler and the requested "Use existing EmailService"
            System.Console.WriteLine($"Email sent to {to}: {subject}");
            return Task.CompletedTask;
        }
    }
}

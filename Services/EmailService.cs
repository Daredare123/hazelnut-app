using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace HazelnutVeb.Services
{
    public class EmailService
    {
        private readonly string _username;
        private readonly string _password;

        public EmailService(IConfiguration configuration)
        {
            _username = configuration["EmailSettings:Username"];
            _password = configuration["EmailSettings:Password"];
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var mail = new MailMessage();
            mail.From = new MailAddress(_username);
            mail.To.Add(to);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = false;

            var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_username, _password),
                EnableSsl = true
            };

            await smtp.SendMailAsync(mail);
        }
    }
}
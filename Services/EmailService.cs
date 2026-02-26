public async Task SendEmailAsync(string to, string subject, string body)
{
    try
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

        Console.WriteLine("EMAIL USPESNO ISPRATEN");
    }
    catch (Exception ex)
    {
        Console.WriteLine("EMAIL ERROR: " + ex.Message);
    }
}
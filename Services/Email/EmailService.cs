namespace TMDTStore.Services;
using System.Net;
using System.Net.Mail;

public class EmailService : IEmailService
{
    private readonly EmailSetting _emailSetting;

    public EmailService(EmailSetting emailSetting)
    {
        _emailSetting = emailSetting;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        using (var client = new SmtpClient(_emailSetting.SmtpHost, _emailSetting.SmtpPort))
        {
            client.Credentials = new NetworkCredential(_emailSetting.Username, _emailSetting.Password);
            client.EnableSsl = _emailSetting.EnableSsl;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSetting.FromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}

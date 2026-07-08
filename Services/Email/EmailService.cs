namespace TMDTStore.Services.Email;

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
        if (string.IsNullOrWhiteSpace(_emailSetting.SmtpHost) ||
            string.IsNullOrWhiteSpace(_emailSetting.FromEmail) ||
            string.IsNullOrWhiteSpace(toEmail))
        {
            throw new InvalidOperationException("Email settings or recipient email is missing.");
        }

        using var message = new MailMessage();
        message.From = new MailAddress(_emailSetting.FromEmail, _emailSetting.FromName);
        message.To.Add(new MailAddress(toEmail));
        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_emailSetting.SmtpHost, _emailSetting.SmtpPort)
        {
            EnableSsl = _emailSetting.EnableSsl,
        };

        if (!string.IsNullOrWhiteSpace(_emailSetting.Username) &&
            !string.IsNullOrWhiteSpace(_emailSetting.Password))
        {
            client.Credentials = new NetworkCredential(_emailSetting.Username, _emailSetting.Password);
        }

        await client.SendMailAsync(message);
    }
}

using System.Text;
using System.Text.Json;

namespace TMDTStore.Services.Email;

public class EmailService : IEmailService
{
    private readonly EmailSetting _emailSetting;
    private readonly HttpClient _httpClient;

    public EmailService(EmailSetting emailSetting, HttpClient httpClient)
    {
        _emailSetting = emailSetting;
        _httpClient = httpClient;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_emailSetting.ApiKey) ||
            string.IsNullOrWhiteSpace(_emailSetting.FromEmail) ||
            string.IsNullOrWhiteSpace(toEmail))
        {
            throw new InvalidOperationException("Email settings (ApiKey, FromEmail) or recipient email is missing.");
        }

        var payload = new
        {
            sender = new { email = _emailSetting.FromEmail, name = _emailSetting.FromName ?? "TVT PC" },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = body
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", _emailSetting.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Brevo API error ({(int)response.StatusCode}): {errorBody}");
        }
    }
}

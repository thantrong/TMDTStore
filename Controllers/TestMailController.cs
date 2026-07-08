using Microsoft.AspNetCore.Mvc;
using TMDTStore.Services.Email;

namespace TMDTStore.Controllers;

public class TestMailController : Controller
{
    private readonly IEmailService _emailService;

    public TestMailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    // GET: /TestMail
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #0033CC, #1A4BFF); padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 24px;'>📧 TVT PC</h1>
                        <p style='color: #A6C1FF; margin: 8px 0 0;'>Linh kiện máy tính chính hãng</p>
                    </div>
                    <div style='background: #fff; padding: 30px; border: 1px solid #E2E8F0; border-top: none; border-radius: 0 0 12px 12px;'>
                        <h2 style='color: #1E293B; font-size: 20px; margin: 0 0 16px;'>Kiểm tra kết nối email</h2>
                        <p style='color: #475569; line-height: 1.6;'>Xin chào,</p>
                        <p style='color: #475569; line-height: 1.6;'>Đây là email kiểm tra từ hệ thống <strong>TVT PC Store</strong>. Nếu bạn nhận được email này, hệ thống gửi email đã hoạt động thành công! 🎉</p>
                        <div style='background: #F0F4FF; border-left: 4px solid #1A4BFF; padding: 16px; margin: 20px 0; border-radius: 8px;'>
                            <p style='color: #475569; margin: 0; font-size: 14px;'>
                                <strong>Thông tin gửi:</strong><br/>
                                ⏰ Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}<br/>
                                📨 Từ: TVT PC &lt;thanvinhtrongsv@gmail.com&gt;<br/>
                                📬 Đến: thanvinhtrongsv@gmail.com
                            </p>
                        </div>
                        <p style='color: #475569; line-height: 1.6;'>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                        <p style='color: #475569; line-height: 1.6; margin-bottom: 0;'>Trân trọng,<br/><strong>Đội ngũ TVT PC</strong></p>
                    </div>
                </div>";

            await _emailService.SendEmailAsync(
                "thanvinhtrongsv@gmail.com",
                "📧 [TVT PC] Kiểm tra kết nối email",
                htmlBody
            );

            return Content("✅ Email test đã được gửi thành công đến thanvinhtrongsv@gmail.com! Vui lòng kiểm tra hộp thư (kể cả Spam).");
        }
        catch (Exception ex)
        {
            return Content($"❌ Gửi email thất bại: {ex.Message}");
        }
    }
}

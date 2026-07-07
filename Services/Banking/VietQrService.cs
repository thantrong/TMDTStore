using Microsoft.Extensions.Options;

namespace TMDTStore.Services.Banking;

public class VietQrService : IVietQrService
{
    private readonly VietQrSetting _settings;

    public VietQrService(IOptions<VietQrSetting> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateContent(string orderId)
    {
        // Nội dung CK: TVT + mã đơn (không dấu, <= 25 ký tự)
        return $"TVT {orderId}";
    }

    public string GenerateQrImageUrl(string orderId, decimal amount)
    {
        var content = Uri.EscapeDataString(GenerateContent(orderId));
        var template = _settings.Template;
        // URL ảnh QR: https://img.vietqr.io/image/{BIN}-{AccountNo}-{template}.png?amount=X&addInfo=Y
        return $"{_settings.ImageApiBaseUrl.TrimEnd('/')}/{_settings.BankId}-{_settings.AccountNo}-{template}.png?amount={(long)amount}&addInfo={content}";
    }
}

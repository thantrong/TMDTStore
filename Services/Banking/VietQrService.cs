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
        return $"TVT {orderId}";
    }

    public string GenerateQrImageUrl(string orderId, decimal amount)
    {
        var content = Uri.EscapeDataString(GenerateContent(orderId));
        var template = _settings.Template;
        return $"{_settings.ImageApiBaseUrl.TrimEnd('/')}/{_settings.BankId}-{_settings.AccountNo}-{template}.png?amount={(long)amount}&addInfo={content}";
    }
}

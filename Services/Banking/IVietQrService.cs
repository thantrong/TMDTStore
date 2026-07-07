namespace TMDTStore.Services.Banking;

public interface IVietQrService
{
    string GenerateQrImageUrl(string orderId, decimal amount);
    string GenerateContent(string orderId);
}

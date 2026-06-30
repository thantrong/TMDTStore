namespace TMDTStore.Services.Cloudinary;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file, string? folder = null);
    Task<bool> DeleteImageAsync(string publicId);
}

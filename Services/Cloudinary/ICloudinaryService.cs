namespace TMDTStore.Services.Cloudinary;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file);
    Task<bool> DeleteImageAsync(string publicId);
}

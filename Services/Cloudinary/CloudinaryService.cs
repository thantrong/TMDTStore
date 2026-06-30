namespace TMDTStore.Services.Cloudinary;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySetting> config)
    {
        var cloudinarySettings = config.Value;
        var account = new Account(
            cloudinarySettings.CloudName,
            cloudinarySettings.ApiKey,
            cloudinarySettings.ApiSecret
        );
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file.Length > 0)
        {
            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.ToString();
        }
        return string.Empty;
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        var deletionParams = new DeletionParams(publicId);
        var deletionResult = await _cloudinary.DestroyAsync(deletionParams);
        return deletionResult.Result == "ok";
    }
}

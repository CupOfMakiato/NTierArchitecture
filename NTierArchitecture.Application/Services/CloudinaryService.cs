using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NTierArchitecture.Application.Abstractions.ThirdPartyService.CloudinaryService;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Application.Settings.CloudinaryService;

namespace NTierArchitecture.Application.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly CloudinarySetting _cloudinarySetting;
        private readonly Cloudinary _cloudinary;
        public CloudinaryService(IOptions<CloudinarySetting> cloudinaryConfig)
        {
            var account = new Account(cloudinaryConfig.Value.CloudName,
                cloudinaryConfig.Value.ApiKey,
                cloudinaryConfig.Value.ApiSecret);

            _cloudinary = new Cloudinary(account);
            _cloudinarySetting = cloudinaryConfig.Value;
        }

        public async Task<DeletionResult> DeleteFileAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId);
            return await _cloudinary.DestroyAsync(deletionParams);
        }

        public async Task<CloudinaryResponse> UploadImage(IFormFile file, string folderName)
        {
            if (file is null)
                return null;
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                PublicId = $"/{Guid.NewGuid()}/{Path.GetFileNameWithoutExtension(file.FileName)}",
                Overwrite = true,
                Folder = folderName
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                return null; // Handle upload failure
            }

            return new CloudinaryResponse
            {
                FileUrl = uploadResult.SecureUrl.ToString(),
            };
        }
    }
}
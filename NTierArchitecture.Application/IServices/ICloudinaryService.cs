using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using NTierArchitecture.Application.Abstractions.ThirdPartyService.CloudinaryService;

namespace NTierArchitecture.Application.IServices
{
    public interface ICloudinaryService
    {
        Task<DeletionResult> DeleteFileAsync(string publicId);
        Task<CloudinaryResponse> UploadImage(IFormFile file, string folderName);
    }
}

using ActioNator.Services.Interfaces.Cloud;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace ActioNator.Services.Implementations.Cloud
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        // Allowed image types and maximum file size (5MB)
        private readonly string[] _allowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
        private const int _maxFileSizeBytes = 5 * 1024 * 1024; // 5MB
        
        public async Task<string> UploadImageAsync(IFormFile file, Guid postId, string folder = "community")
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file was provided", nameof(file));
            }

            // Check if the file is an allowed image type
            if (!_allowedImageTypes.Contains(file.ContentType.ToLower()))
            {
                throw new ArgumentException($"File type '{file.ContentType}' is not allowed. Allowed types: {string.Join(", ", _allowedImageTypes)}", nameof(file));
            }
            
            // Check file size
            if (file.Length > _maxFileSizeBytes)
            {
                throw new ArgumentException($"File size exceeds the maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)}MB", nameof(file));
            }

            // Create a unique filename with timestamp to support multiple images per post
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var uniqueId = Guid.NewGuid().ToString().Substring(0, 8); // First 8 chars of a new GUID
            var publicId = $"{folder}/{postId}/{timestamp}_{uniqueId}";

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),

                PublicId = publicId,

                UseFilename = false,     
                UniqueFilename = false,  
                Overwrite = true,        

                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new InvalidOperationException($"Failed to upload image: {uploadResult.Error.Message}");

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
            {
                throw new ArgumentException("Public ID cannot be empty", nameof(publicId));
            }

            DeletionParams deleteParams = new (publicId);
            DeletionResult result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }

        public string GetPublicIdFromUrl(string cloudinaryUrl)
        {
            if (string.IsNullOrEmpty(cloudinaryUrl))
            {
                return string.Empty;
            }

            Match match 
                = Regex.Match(cloudinaryUrl, @"\/v\d+\/(.+)\.");

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }
    }
}

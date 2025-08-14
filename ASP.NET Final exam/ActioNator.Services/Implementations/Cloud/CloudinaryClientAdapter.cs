using System.Threading;
using System.Threading.Tasks;
using ActioNator.Services.Interfaces.Cloud;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace ActioNator.Services.Implementations.Cloud
{
    /// <summary>
    /// Default implementation delegating to Cloudinary SDK.
    /// </summary>
    public sealed class CloudinaryClientAdapter : ICloudinaryClientAdapter
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryClientAdapter(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary ?? throw new System.ArgumentNullException(nameof(cloudinary));
        }

        public Task<ImageUploadResult> UploadAsync(ImageUploadParams uploadParams, CancellationToken cancellationToken = default)
            => _cloudinary.UploadAsync(uploadParams, cancellationToken);

        public Task<DelResResult> DeleteResourcesAsync(DelResParams delResParams, CancellationToken cancellationToken = default)
            => _cloudinary.DeleteResourcesAsync(delResParams, cancellationToken);
    }
}

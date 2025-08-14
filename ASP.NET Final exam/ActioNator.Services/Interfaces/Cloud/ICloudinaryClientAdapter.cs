using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet.Actions;

namespace ActioNator.Services.Interfaces.Cloud
{
    /// <summary>
    /// Abstraction over Cloudinary operations used by CloudinaryService to enable mocking in tests.
    /// </summary>
    public interface ICloudinaryClientAdapter
    {
        Task<ImageUploadResult> UploadAsync(ImageUploadParams uploadParams, CancellationToken cancellationToken = default);
        Task<DelResResult> DeleteResourcesAsync(DelResParams delResParams, CancellationToken cancellationToken = default);
    }
}

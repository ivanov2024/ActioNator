using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Services.Implementations.Cloud;
using ActioNator.Services.Interfaces.Cloud;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using static ActioNator.GCommon.FileConstants.ContentTypes;

/// <summary>
/// Handles Cloudinary image uploads and deletions related to posts.
/// </summary>
public class CloudinaryService : ICloudinaryService
{
    private readonly ICloudinaryClientAdapter _client;
    private readonly ICloudinaryUrlService _cloudinaryUrlService;
    private readonly ActioNatorDbContext _dbContext;
    private readonly ILogger<CloudinaryService> _logger;

    //10 MB
    private const int _maxFileSizeBytes = 10 * 1024 * 1024;

    public CloudinaryService(Cloudinary cloudinary, ActioNatorDbContext dbContext, ILogger<CloudinaryService> logger, ICloudinaryUrlService cloudinaryUrlService)
    {
        if (cloudinary is null) throw new ArgumentNullException(nameof(cloudinary));
        _client = new CloudinaryClientAdapter(cloudinary);

        _cloudinaryUrlService = cloudinaryUrlService 
            ?? throw new ArgumentNullException( nameof(cloudinaryUrlService));

        _dbContext = dbContext 
            ?? throw new ArgumentNullException(nameof(dbContext));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public CloudinaryService(ICloudinaryClientAdapter client, ActioNatorDbContext dbContext, ILogger<CloudinaryService> logger, ICloudinaryUrlService cloudinaryUrlService)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));

        _cloudinaryUrlService = cloudinaryUrlService 
            ?? throw new ArgumentNullException( nameof(cloudinaryUrlService));

        _dbContext = dbContext 
            ?? throw new ArgumentNullException(nameof(dbContext));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> UploadImageAsync
    (
        IFormFile file,
        Guid postId,
        string folder = "community",
        CancellationToken cancellationToken = default
    )
    {
        // Upload single file and set Post.ImageUrl
        string imageUrl = await UploadToCloudinaryAsync(file, postId, folder, cancellationToken);

        Post? post = await _dbContext
            .Posts
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

        if (post == null)
        {
            _logger.LogError("Post with id {PostId} was not found during image upload.", postId);
            throw new ArgumentNullException($"Post with ID {postId} not found.");
        }

        post.ImageUrl = imageUrl;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }

    public async Task<IEnumerable<string>> UploadImagesAsync(IEnumerable<IFormFile> files, Guid postId, string folder = "community", CancellationToken cancellationToken = default)
    {
        if (files == null || !files.Any())
            throw new ArgumentException("No files were provided", nameof(files));

        // Normalize to list to evaluate count once
        List<IFormFile> fileList = files.Where(f => f != null).ToList();
        if (fileList.Count == 0)
            throw new ArgumentException("No files were provided", nameof(files));

        // Single image: set Post.ImageUrl, do not create PostImages
        if (fileList.Count == 1)
        {
            string singleUrl = await UploadImageAsync(fileList[0], postId, folder, cancellationToken);
            return new List<string> { singleUrl };
        }

        // Multiple images: upload each and create PostImage entities
        List<string> uploadedUrls = new();

        foreach (IFormFile file in fileList)
        {
            string url = await UploadToCloudinaryAsync(file, postId, folder, cancellationToken);
            uploadedUrls.Add(url);
        }

        foreach (string url in uploadedUrls)
        {
            PostImage postImage = new()
            {
                Id = Guid.NewGuid(),
                ImageUrl = url,
                PostId = postId,
            };

            await _dbContext.PostImages.AddAsync(postImage, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return uploadedUrls;
    }

    public async Task<bool> DeleteImagesByPublicIdsAsync(Guid postId, List<string> publicIds, CancellationToken cancellationToken)
    {
        if (publicIds == null || publicIds.Count == 0)
        {
            return true; // Nothing to delete
        }

        DelResParams deleteParams = new()
        {
            PublicIds = publicIds
        };

        DelResResult deleteResult 
            = await 
            _client
            .DeleteResourcesAsync(deleteParams, cancellationToken);

        bool allDeleted 
            = deleteResult
            .Deleted?
            .Values
            .All(v => v == "deleted") ?? false;

        if (allDeleted)
        {
            // Fetch all PostImage entities linked to the post
            List<PostImage>? postImages 
                = await _dbContext
                .PostImages
                .Where(pi => pi.PostId == postId)
                .ToListAsync(cancellationToken);

            // Filter the PostImages to delete based on matching publicIds
            List<PostImage>? imagesToDelete 
                = postImages
                .Where(pi => publicIds
                    .Contains(_cloudinaryUrlService
                        .GetPublicId(pi.ImageUrl)))
                .ToList();

            if (imagesToDelete.Count != 0)
            {
                imagesToDelete
                    .ForEach(itd =>
                        _dbContext
                        .PostImages
                        .Remove(itd)
                    );

                await 
                    _dbContext
                    .SaveChangesAsync(cancellationToken);
            }
        }

        return allDeleted;
    }

    #region Private Helper Methods
    private async Task<string> UploadToCloudinaryAsync(
        IFormFile file,
        Guid postId,
        string folder,
        CancellationToken cancellationToken)
    {
        ValidateFile(file);

        string publicId = GeneratePublicId(folder, postId);

        await using Stream stream = file.OpenReadStream();

        ImageUploadParams uploadParams = new()
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

        ImageUploadResult uploadResult = await _client.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.Error != null)
        {
            _logger.LogCritical(
                "Image uploading failed for postId {PostId}. Error: {ErrorMessage}",
                postId,
                uploadResult.Error.Message);

            throw new ArgumentException($"Failed to upload image: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl.ToString();
    }
    private static void ValidateFile(IFormFile image)
    {
        if (image == null || image.Length == 0)
            throw new ArgumentException("No file was provided", nameof(image));

        if (!IsSupported(image.ContentType))
            throw new ArgumentException($"File type '{image.ContentType}' is not allowed. Allowed types: {string.Join(", ", Supported)}", nameof(image));

        if (image.Length > _maxFileSizeBytes)
            throw new ArgumentException($"File size exceeds the maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)}MB", nameof(image));
    }

    private static string GeneratePublicId(string folder, Guid postId)
    {
        string timestamp = DateTime.UtcNow
            .ToString("yyyyMMddHHmmssfff");
        string uniqueId 
            = Guid.NewGuid()
            .ToString("N")
            .Substring(0, 8);

        return $"{folder}/{postId}/{timestamp}_{uniqueId}";
    }
    #endregion
}

public class ListResourcesWithPrefixParams : ListResourcesParams
{
    public string Prefix { get; }

    public ListResourcesWithPrefixParams(string prefix)
        => Prefix = prefix;
   
    public override SortedDictionary<string, object> ToParamsDictionary()
    {
        SortedDictionary<string, object> dict 
            = base.ToParamsDictionary();

        if (!string.IsNullOrWhiteSpace(Prefix))
        {
            // Add the prefix parameter manually
            dict["prefix"] = Prefix;
        }

        return dict;
    }
}

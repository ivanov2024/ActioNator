using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces.FileServices
{
    /// <summary>
    /// Dropbox-specific file storage operations
    /// </summary>
    public interface IDropboxFileStorageService : IFileStorageService
    {
        Task<string> ReplaceFileAsync(string dropboxPath, IFormFile newFile, CancellationToken cancellationToken = default);
        Task<bool> DeleteFileAsync(string dropboxPath, CancellationToken cancellationToken = default);
        Task<string> GetSharedLinkAsync(string dropboxPath, CancellationToken cancellationToken = default);
    }
}

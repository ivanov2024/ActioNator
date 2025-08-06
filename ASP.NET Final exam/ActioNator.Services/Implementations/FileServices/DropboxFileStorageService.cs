using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ActioNator.Services.Interfaces.FileServices;

namespace ActioNator.Services.Implementations.FileServices
{
    /// <summary>
    /// Dropbox-based implementation of IFileStorageService
    /// </summary>
    public class DropboxFileStorageService : IDropboxFileStorageService
    {
        private readonly string _accessToken;
        private readonly HttpClient _httpClient;

        public DropboxFileStorageService(string accessToken)
        {
            _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task<IEnumerable<string>> SaveFilesAsync(IFormFileCollection files, string relativePath, string userId, CancellationToken cancellationToken = default)
        {
            var urls = new List<string>();
            foreach (var file in files)
            {
                // Validate file type and size here (implement as needed)
                // Save to Dropbox
                var url = await UploadFileToDropboxAsync(file, relativePath, userId, cancellationToken);
                urls.Add(url);
            }
            return urls;
        }

        public string GetSafeFileName(string fileName)
        {
            // Implement file name sanitization logic
            return Path.GetFileName(fileName);
        }

        public async Task<(Stream FileStream, string ContentType)> GetFileAsync(string filePath, string userId, CancellationToken cancellationToken = default)
        {
            // Implement Dropbox file download logic
            throw new NotImplementedException();
        }

        public async Task<bool> IsUserAuthorizedForFileAsync(string filePath, string userId)
        {
            // Implement authorization logic if needed
            return true;
        }

        private async Task<string> UploadFileToDropboxAsync(IFormFile file, string relativePath, string userId, CancellationToken cancellationToken)
        {
            var dropboxPath = $"/{relativePath}/{userId}/{GetSafeFileName(file.FileName)}";
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;
                var content = new ByteArrayContent(ms.ToArray());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var request = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload")
                {
                    Content = content
                };
                request.Headers.Add("Dropbox-API-Arg", $"{{\"path\": \"{dropboxPath}\", \"mode\": \"overwrite\", \"autorename\": false, \"mute\": false, \"strict_conflict\": false}}");
                request.Headers.Add("Content-Type", "application/octet-stream");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                // Parse Dropbox response and return the shared link or file path
                // For now, return the Dropbox path as a placeholder
                return dropboxPath;
            }
        }

                public async Task<string> ReplaceFileAsync(string dropboxPath, IFormFile newFile, CancellationToken cancellationToken = default)
        {
            // Delete old file and upload new one
            await DeleteFileAsync(dropboxPath, cancellationToken);
            var url = await UploadFileToDropboxAsync(newFile, Path.GetDirectoryName(dropboxPath), "", cancellationToken);
            return url;
        }

        public async Task<bool> DeleteFileAsync(string dropboxPath, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/files/delete_v2")
            {
                Content = new StringContent($"{{\"path\": \"{dropboxPath}\"}}")
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetSharedLinkAsync(string dropboxPath, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/sharing/create_shared_link_with_settings")
            {
                Content = new StringContent($"{{\"path\": \"{dropboxPath}\"}}")
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            // Parse json to extract the url
            var url = System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("url").GetString();
            return url;
        }

        // Example validation method for images
        public bool IsValidImage(IFormFile file, out string error)
        {
            error = string.Empty;
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            var maxSize = 5 * 1024 * 1024; // 5MB
            if (!allowedTypes.Contains(file.ContentType))
            {
                error = "Invalid file type. Only JPEG, PNG, and GIF are allowed.";
                return false;
            }
            if (file.Length > maxSize)
            {
                error = "File size exceeds the 5MB limit.";
                return false;
            }
            return true;
        }
    }
}

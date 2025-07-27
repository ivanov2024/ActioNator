using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces
{
    /// <summary>
    /// Interface for abstracting file system operations to improve testability
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Checks if a file exists at the specified path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if the file exists, false otherwise</returns>
        bool FileExists(string path);
        
        /// <summary>
        /// Checks if a directory exists at the specified path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if the directory exists, false otherwise</returns>
        bool DirectoryExists(string path);
        
        /// <summary>
        /// Creates a directory at the specified path if it doesn't exist
        /// </summary>
        /// <param name="path">Path where to create the directory</param>
        void CreateDirectory(string path);
        
        /// <summary>
        /// Opens a file for reading
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Stream for reading the file</returns>
        Stream OpenRead(string path);
        
        /// <summary>
        /// Opens a file for writing
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Stream for writing to the file</returns>
        Stream OpenWrite(string path);
        
        /// <summary>
        /// Creates a new file and opens it for writing
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Stream for writing to the file</returns>
        Stream Create(string path);
        
        /// <summary>
        /// Copies data from one stream to another asynchronously
        /// </summary>
        /// <param name="source">Source stream</param>
        /// <param name="destination">Destination stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task CopyToAsync(Stream source, Stream destination, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the file name from a path
        /// </summary>
        /// <param name="path">Path to extract file name from</param>
        /// <returns>File name</returns>
        string GetFileName(string path);
        
        /// <summary>
        /// Gets the file extension from a path
        /// </summary>
        /// <param name="path">Path to extract file extension from</param>
        /// <returns>File extension</returns>
        string GetExtension(string path);
        
        /// <summary>
        /// Combines path segments into a single path
        /// </summary>
        /// <param name="paths">Path segments to combine</param>
        /// <returns>Combined path</returns>
        string CombinePaths(params string[] paths);
    }
}

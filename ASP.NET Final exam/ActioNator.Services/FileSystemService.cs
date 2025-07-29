using ActioNator.Services.Interfaces;

namespace ActioNator.Services
{
    /// <summary>
    /// Implementation of IFileSystem that wraps System.IO operations
    /// </summary>
    public class FileSystemService : IFileSystem
    {
        /// <summary>
        /// Checks if a file exists at the specified path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if the file exists, false otherwise</returns>
        public bool FileExists(string path) => File.Exists(path);
        
        /// <summary>
        /// Checks if a directory exists at the specified path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if the directory exists, false otherwise</returns>
        public bool DirectoryExists(string path) => Directory.Exists(path);
        
        /// <summary>
        /// Creates a directory at the specified path if it doesn't exist
        /// </summary>
        /// <param name="path">Path where to create the directory</param>
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        
        /// <summary>
        /// Opens a file for reading
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Stream for reading the file</returns>
        public Stream OpenRead(string path) => File.OpenRead(path);
        
        /// <summary>
        /// Opens a file for writing
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Stream for writing to the file</returns>
        public Stream OpenWrite(string path) => File.OpenWrite(path);
        
        /// <summary>
        /// Creates a new file and opens it for writing
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Stream for writing to the file</returns>
        public Stream Create(string path) => File.Create(path);
        
        /// <summary>
        /// Copies data from one stream to another asynchronously
        /// </summary>
        /// <param name="source">Source stream</param>
        /// <param name="destination">Destination stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task CopyToAsync(Stream source, Stream destination, CancellationToken cancellationToken) 
            => source.CopyToAsync(destination, cancellationToken);
        
        /// <summary>
        /// Gets the file name from a path
        /// </summary>
        /// <param name="path">Path to extract file name from</param>
        /// <returns>File name</returns>
        public string GetFileName(string path) => Path.GetFileName(path);
        
        /// <summary>
        /// Gets the file extension from a path
        /// </summary>
        /// <param name="path">Path to extract file extension from</param>
        /// <returns>File extension</returns>
        public string GetExtension(string path) => Path.GetExtension(path);
        
        /// <summary>
        /// Combines path segments into a single path
        /// </summary>
        /// <param name="paths">Path segments to combine</param>
        /// <returns>Combined path</returns>
        public string CombinePaths(params string[] paths) => Path.Combine(paths);
    }
}

using ActioNator.Services.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ActioNator.Middleware
{
    /// <summary>
    /// Middleware to handle file upload related exceptions globally
    /// </summary>
    public class FileUploadExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FileUploadExceptionMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the FileUploadExceptionMiddleware class
        /// </summary>
        /// <param name="next">The next middleware in the pipeline</param>
        /// <param name="logger">Logger instance</param>
        public FileUploadExceptionMiddleware(
            RequestDelegate next,
            ILogger<FileUploadExceptionMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (
                ex is FileValidationException ||
                ex is FileStorageException ||
                ex is FileContentTypeException ||
                ex is FileSizeExceededException ||
                ex is FileNameValidationException)
            {
                _logger.LogError(ex, "File upload exception occurred: {Message}", ex.Message);
                await HandleFileExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Handles file upload exceptions
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <param name="exception">The exception</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private static Task HandleFileExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";
            
            int statusCode = exception switch
            {
                FileSizeExceededException => (int)HttpStatusCode.RequestEntityTooLarge, // 413
                FileContentTypeException => (int)HttpStatusCode.UnsupportedMediaType, // 415
                FileNameValidationException => (int)HttpStatusCode.BadRequest, // 400
                FileValidationException => (int)HttpStatusCode.BadRequest, // 400
                FileStorageException => (int)HttpStatusCode.InternalServerError, // 500
                _ => (int)HttpStatusCode.InternalServerError // 500
            };
            
            context.Response.StatusCode = statusCode;

            var problemDetails = new
            {
                type = exception.GetType().Name,
                title = GetExceptionTitle(exception),
                status = statusCode,
                detail = exception.Message,
                instance = context.Request.Path
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(problemDetails, options);
            return context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Gets a user-friendly title for the exception
        /// </summary>
        /// <param name="exception">The exception</param>
        /// <returns>A user-friendly title</returns>
        private static string GetExceptionTitle(Exception exception) => exception switch
        {
            FileSizeExceededException => "File Size Limit Exceeded",
            FileContentTypeException => "Invalid File Type",
            FileNameValidationException => "Invalid File Name",
            FileValidationException => "File Validation Failed",
            FileStorageException => "File Storage Error",
            _ => "File Upload Error"
        };
    }

    /// <summary>
    /// Extension methods for the FileUploadExceptionMiddleware
    /// </summary>
    public static class FileUploadExceptionMiddlewareExtensions
    {
        /// <summary>
        /// Adds the FileUploadExceptionMiddleware to the application pipeline
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseFileUploadExceptionHandler(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FileUploadExceptionMiddleware>();
        }
    }
}

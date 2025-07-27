using ActioNator.Services.Configuration;
using ActioNator.Services.ContentInspectors;
using ActioNator.Services.Interfaces;
using ActioNator.Services.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ActioNator.Services.Extensions
{
    /// <summary>
    /// Extension methods for configuring services in the DI container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds file upload services to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddFileUploadServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration options
            services.Configure<FileUploadOptions>(configuration.GetSection("FileUpload"));

            // Register file system abstraction
            services.AddSingleton<IFileSystem, FileSystemService>();

            // Register content inspectors
            services.AddSingleton<IFileContentInspector, ImageContentInspector>();
            services.AddSingleton<IFileContentInspector, PdfContentInspector>();

            // Register validators
            services.AddSingleton<ImageFileValidator>();
            services.AddSingleton<PdfFileValidator>();
            services.AddSingleton<IFileValidator, ImageFileValidator>();
            services.AddSingleton<IFileValidator, PdfFileValidator>();

            // Register factory and orchestrator
            services.AddSingleton<IFileValidatorFactory, FileValidatorFactory>();
            services.AddSingleton<IFileValidationOrchestrator, FileValidationOrchestrator>();

            // Register storage service
            services.AddSingleton<IFileStorageService, FileStorageService>();

            // Register coach document upload service
            services.AddScoped<ICoachDocumentUploadService, CoachDocumentUploadService>();

            return services;
        }
    }
}

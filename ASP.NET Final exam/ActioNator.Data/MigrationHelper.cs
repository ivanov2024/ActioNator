using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace ActioNator.Data
{
    /// <summary>
    /// Helper class for migrations that provides a standalone service provider
    /// without any SignalR dependencies
    /// </summary>
    public static class MigrationHelper
    {
        /// <summary>
        /// Creates a standalone service provider for migrations
        /// </summary>
        public static IServiceProvider CreateStandaloneServiceProvider(string connectionString = null)
        {
            // Create a service collection
            var services = new ServiceCollection();

            // If no connection string is provided, try to get it from configuration
            if (string.IsNullOrEmpty(connectionString))
            {
                // Get the configuration from appsettings.json
                string projectDir = Directory.GetCurrentDirectory();
                
                // If we're in the Data project, navigate up to the ActioNator project
                if (projectDir.EndsWith("ActioNator.Data"))
                {
                    projectDir = Path.GetFullPath(Path.Combine(projectDir, "..", "ActioNator"));
                }
                
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(projectDir)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .Build();

                // Get the connection string from configuration
                connectionString = configuration.GetConnectionString("DefaultActioNatorConnection");
                
                // Fallback to hardcoded connection string if not found in config
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Use the same connection string as defined in ActioNatorConnectionString.cs
                    connectionString = "Server=.;Database=ActioNator;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
                }
            }

            // Register the DbContext with the connection string and specify migrations assembly
            services.AddDbContext<ActioNatorDbContext>(options =>
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly("ActioNator.Data")));

            // Build and return the service provider
            return services.BuildServiceProvider();
        }
    }
}

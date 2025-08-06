using ActioNator.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace ActioNator
{
    /// <summary>
    /// Design-time factory for ActioNatorDbContext to facilitate EF Core migrations
    /// without requiring runtime services like SignalR
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ActioNatorDbContext>
    {
        public ActioNatorDbContext CreateDbContext(string[] args)
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
            string connectionString = configuration.GetConnectionString("DefaultActioNatorConnection");
            
            // Fallback to hardcoded connection string if not found in config
            if (string.IsNullOrEmpty(connectionString))
            {
                // Use the same connection string as defined in ActioNatorConnectionString.cs
                connectionString = "Server=.;Database=ActioNator;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
            }

            // Create a service collection that only includes the minimum required services
            // This avoids the SignalR dependency issue during migrations
            var serviceCollection = new ServiceCollection();
            
            // Register only the DbContext with the connection string
            serviceCollection.AddDbContext<ActioNatorDbContext>(options =>
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly("ActioNator.Data")));
            
            // Build the service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            // Get the DbContext from the service provider
            return serviceProvider.GetRequiredService<ActioNatorDbContext>();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace ActioNator.Data
{
    /// <summary>
    /// Design-time factory for ActioNatorDbContext to support EF Core migrations
    /// without requiring runtime services like SignalR
    /// </summary>
    public class ActioNatorDbContextFactory : IDesignTimeDbContextFactory<ActioNatorDbContext>
    {
        public ActioNatorDbContext CreateDbContext(string[] args)
        {
            Console.WriteLine("Using ActioNatorDbContextFactory for migrations");
            
            // Create a standalone service provider using MigrationHelper
            var serviceProvider = MigrationHelper.CreateStandaloneServiceProvider();
            
            // Get the DbContext from the service provider
            return serviceProvider.GetRequiredService<ActioNatorDbContext>();
        }
    }
}

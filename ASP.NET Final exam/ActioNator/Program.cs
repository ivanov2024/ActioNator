using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Middleware;
using ActioNator.Services.Configuration;
using ActioNator.Services.ContentInspectors;
using ActioNator.Services.Implementations.AuthenticationService;
using ActioNator.Services.Implementations.FileServices;
using ActioNator.Services.Implementations.VerifyCoach;
using ActioNator.Services.Interfaces.AuthenticationServices;
using ActioNator.Services.Interfaces.FileServices;
using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.Services.Seeding;
using ActioNator.Services.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ActioNator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            string connectionString 
                = builder
                .Configuration
                .GetConnectionString("DefaultActioNatorConnection") 
                ?? 
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ActioNatorDbContext>
            (options =>
                options.UseSqlServer(connectionString)
            );

            builder.Services
                .AddDatabaseDeveloperPageExceptionFilter();

            builder.Services
                .AddControllersWithViews();

            builder.Services
                .AddRazorPages();

            // Register configuration options
            builder.Services
                .Configure<FileUploadOptions>
                (builder.Configuration.GetSection("FileUpload"));
            
            // Register file system and content inspectors
            builder.Services
                .AddScoped<IFileSystem, FileSystemService>();
            builder.Services
                .AddScoped<ImageContentInspector>();
            builder.Services
                .AddScoped<PdfContentInspector>();
            
            // Register content inspectors as IFileContentInspector (last one wins for interface resolution)
            builder.Services
                .AddScoped<IFileContentInspector>(sp => sp.GetRequiredService<ImageContentInspector>());
            builder.Services
                .AddScoped<IFileContentInspector>(sp => sp.GetRequiredService<PdfContentInspector>());
            
            // Register validators with specific named registrations to avoid DI conflicts
            builder.Services.AddScoped<ImageFileValidator>(sp => 
                new ImageFileValidator(
                    sp
                    .GetRequiredService<IOptions<FileUploadOptions>>(),
                    sp
                    .GetRequiredService<ILogger<ImageFileValidator>>(),
                    sp
                    .GetRequiredService<IFileSystem>(),
                    sp
                    .GetRequiredService<ImageContentInspector>()
                ));
            
            builder.Services.AddScoped<PdfFileValidator>(sp => 
                new PdfFileValidator(
                    sp
                    .GetRequiredService<IOptions<FileUploadOptions>>(),
                    sp
                    .GetRequiredService<ILogger<PdfFileValidator>>(),
                    sp
                    .GetRequiredService<IFileSystem>(),
                    sp
                    .GetRequiredService<PdfContentInspector>()
                ));
                
            // Register concrete validators as IFileValidator
            builder.Services
                .AddScoped<IFileValidator>(sp => sp.GetRequiredService<ImageFileValidator>());
            builder.Services
                .AddScoped<IFileValidator>(sp => sp.GetRequiredService<PdfFileValidator>());
            
            // Register factory and orchestrator using factory methods
            builder.Services
                .AddScoped<IFileValidatorFactory>(sp => 
                new FileValidatorFactory(sp)
            );
                
            builder.Services.AddScoped<IFileValidationOrchestrator>(sp => 
                new FileValidationOrchestrator(
                    sp
                    .GetRequiredService<IFileValidatorFactory>(),
                    sp
                    .GetRequiredService<ILogger<FileValidationOrchestrator>>()
                ));
            
            // Register services
            builder.Services
                .AddScoped<IFileStorageService, FileStorageService>();
            builder.Services
                .AddScoped<ICoachDocumentUploadService, CoachDocumentUploadService>();
            
            // Register authentication service
            builder.Services
                .AddScoped<IAuthenticationService, AuthenticationService>();

     

            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<ActioNatorDbContext>()
                .AddDefaultTokenProviders();

            WebApplication app = builder.Build();

            AsyncServiceScope asyncServiceScope 
                = app.Services.CreateAsyncScope();

            using (IServiceScope scope = asyncServiceScope)
            {
                RoleManager<IdentityRole<Guid>> roleManager 
                    = scope
                    .ServiceProvider
                    .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

                RoleSeeder roleSeeder 
                    = new (roleManager);

                await roleSeeder.SeedRolesAsync();
            }

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.Redirect("/Identity/Account/Access");
                    return;
                }
                await next();
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            // Register custom exception middleware
            app.UseFileUploadExceptionHandler();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();

            await app.RunAsync();
        }
    }
}

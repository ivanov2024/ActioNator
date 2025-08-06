using ActioNator.Configuration;
using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Hubs;
using ActioNator.Middleware;
using ActioNator.Services.Configuration;
using ActioNator.Services.ContentInspectors;
using ActioNator.Services.Implementations.AuthenticationService;
using ActioNator.Services.Implementations.Cloud;
using ActioNator.Services.Implementations.Communication;
using ActioNator.Services.Implementations.Community;
using ActioNator.Services.Implementations.FileServices;
using ActioNator.Services.Implementations.GoalService;
using ActioNator.Services.Implementations.InputSanitization;
using ActioNator.Services.Implementations.JournalService;
using ActioNator.Services.Implementations.UserDashboard;
using ActioNator.Services.Implementations.UserProfileService;
using ActioNator.Services.Implementations.VerifyCoach;
using ActioNator.Services.Implementations.WorkoutService;
using ActioNator.Services.Interfaces.AuthenticationServices;
using ActioNator.Services.Interfaces.Cloud;
using ActioNator.Services.Interfaces.Communication;
using ActioNator.Services.Interfaces.Community;
using ActioNator.Services.Interfaces.FileServices;
using ActioNator.Services.Interfaces.GoalService;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.Services.Interfaces.JournalService;
using ActioNator.Services.Interfaces.UserDashboard;
using ActioNator.Services.Interfaces.UserProfileService;
using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.Services.Interfaces.WorkoutService;
using ActioNator.Services.Seeding;
using ActioNator.Services.Validators;
using CloudinaryDotNet;
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

            // Register DropboxFileStorageService for IDropboxFileStorageService
            builder.Services.AddScoped<IDropboxFileStorageService>(provider =>
            {
                var dropboxAccessToken = builder.Configuration["Dropbox:AccessToken"];
                if (string.IsNullOrEmpty(dropboxAccessToken))
                {
                    throw new InvalidOperationException("Dropbox access token is not configured. Please set the 'Dropbox:AccessToken' in appsettings.json");
                }
                return new DropboxFileStorageService(dropboxAccessToken);
            });

            // Add services to the container.
            string connectionString 
                = builder
                .Configuration
                .GetConnectionString("DefaultActioNatorConnection") 
                ?? 
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ActioNatorDbContext>
            (options =>
                options.UseSqlServer(connectionString, b 
                => b.MigrationsAssembly("ActioNator.Data"))
            );

            builder.Services
                .AddDatabaseDeveloperPageExceptionFilter();

            builder.Services
                .AddControllersWithViews();

            builder.Services
                .AddRazorPages()
                .AddRazorPagesOptions(options =>
                {
                    // Configure Identity area pages
                    options.Conventions.AuthorizeAreaFolder("Identity", "/Account");
                    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Access");
                    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/AccessDenied");
                })
                .AddRazorRuntimeCompilation();
                
            // Add SignalR services
            builder.Services.AddSignalR();

            // Register configuration options
            builder.Services
                .Configure<FileUploadOptions>
                (builder.Configuration.GetSection("FileUpload"));
                
            // Register Cloudinary configuration
            builder.Services
                .Configure<CloudinarySettings>
                (builder.Configuration.GetSection("Cloudinary"));
                
            // Register Cloudinary service
            builder.Services
                .AddScoped<ICloudinaryService, CloudinaryService>();
            
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

            // Register user dashboard service
            builder.Services
                .AddScoped<IUserDashboardService, UserDashboardService>();

            builder.Services
                .AddScoped<IWorkoutService, WorkoutService>();

            builder.Services
                .AddScoped<IInputSanitizationService, InputSanitizationService>();

            builder.Services
                .AddScoped<IGoalService, GoalService>();

            // Register Journal service
            builder.Services
                .AddScoped<IJournalService, JournalService>();

            builder
                .Services
                .AddScoped<ICommunityService, CommunityService>();

            builder
                .Services
                .AddScoped<IUserProfileService, UserProfileService>();

            // Check if we're running in migration mode
            bool isMigrationMode = args.Contains("--design-time") || 
                                  AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName?.Contains("Microsoft.EntityFrameworkCore.Design") == true) ||
                                  Environment.GetEnvironmentVariable("ACTIO_NATOR_MIGRATION_MODE") == "true";
            
            // Register SignalRService with conditional dependency injection
            if (isMigrationMode)
            {
                // For design-time operations (migrations), register the null implementation
                builder.Services.AddScoped<ISignalRService, NullSignalRService>();
                
                // Log that we're using the null implementation
                Console.WriteLine("Using NullSignalRService for design-time operations");
            }
            else
            {
                // For runtime operations, register the real implementation
                builder.Services.AddScoped<ISignalRService, SignalRService>();
            }

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

            builder.Services.AddAntiforgery(options => 
            {
                // Set the cookie name and header name
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = "CSRF-TOKEN";
                options.FormFieldName = "__RequestVerificationToken";
            });

            // Configure authorization options
            builder.Services.ConfigureApplicationCookie(options =>
            {
                // Set the login path
                options.LoginPath = "/Identity/Account/Access";
                
                // Set the access denied path
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                
                // Set cookie expiration
                options.ExpireTimeSpan = TimeSpan.FromHours(2);
                options.SlidingExpiration = true;
            });

            builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

            builder.Services.Configure<CloudinarySettings>(
                builder.Configuration.GetSection("CloudinarySettings"));

            builder.Services
                .AddSingleton(provider =>
            {
                CloudinarySettings config 
                = provider
                .GetRequiredService<IOptions<CloudinarySettings>>().Value;

                Account account 
                = new (config.CloudName, config.ApiKey, config.ApiSecret);

                return new Cloudinary(account);
            });

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
                    = new(roleManager);

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

            // Map area route with higher priority
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                
            // Map Razor Pages
            app.MapRazorPages();
            
            // Map SignalR hubs
            app.MapHub<CommunityHub>("/communityHub");

            await app.RunAsync();      
        }
    }
}

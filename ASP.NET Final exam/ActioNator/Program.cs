using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Hubs;
using ActioNator.Infrastructure.Settings;
using ActioNator.Middleware;
using ActioNator.Services.Configuration;
using ActioNator.Services.ContentInspectors;
using ActioNator.Extensions;
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
using ActioNator.Services.Interfaces;
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
using ActioNator.Services.Interfaces.Security;
using ActioNator.Services.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;

namespace ActioNator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Ensure App_Data directory exists
            var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            // Ensure profile pictures and cover photos directories exist
            var profilePicsPath = Path.Combine(appDataPath, "profile-pictures");
            var coverPhotosPath = Path.Combine(appDataPath, "cover-photos");
            if (!Directory.Exists(profilePicsPath))
            {
                Directory.CreateDirectory(profilePicsPath);
            }
            if (!Directory.Exists(coverPhotosPath))
            {
                Directory.CreateDirectory(coverPhotosPath);
            }

            // Add services to the container.
            string connectionString 
                = builder
                .Configuration
                .GetConnectionString("DefaultActioNatorConnection") 
                ?? 
                throw new InvalidOperationException("Connection string 'DefaultActioNatorConnection' not found.");

            builder.Services.AddDbContext<ActioNatorDbContext>
            (options =>
                options.UseSqlServer(connectionString, b 
                => b.MigrationsAssembly("ActioNator.Data"))
            );

            builder.Services
                .AddDatabaseDeveloperPageExceptionFilter();

            builder.Services
                .AddControllersWithViews();

            // Ensure built-in inline route constraints like 'regex' are registered
            builder.Services.AddRouting(options =>
            {
                // Map the 'regex' inline constraint to the framework implementation
                options.ConstraintMap["regex"] = typeof(RegexRouteConstraint);
            });

            // Centralized authorization policies for role-based alias routing
            builder.Services.AddAuthorization(options =>
            {
                // Coaches (and Admins) can access User area via /Coach/* aliases
                options.AddPolicy("CoachAreaAliasAccess", policy =>
                    policy.RequireRole("Coach", "Admin"));

                // Admins can access User area via /Admin/* aliases (without duplicating controllers)
                options.AddPolicy("AdminAreaAliasAccess", policy =>
                    policy.RequireRole("Admin"));
            });

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
            
            // Data Protection for encrypting sensitive tokens
            builder.Services.AddDataProtection();
                
            // Add SignalR services
            builder.Services.AddSignalR();

            // Register configuration options
            builder.Services
                .Configure<FileUploadOptions>
                (builder.Configuration.GetSection("FileUpload"));
            
            // Bind Dropbox options
            builder.Services
                .Configure<DropboxOptions>
                (builder.Configuration.GetSection(DropboxOptions.SectionName));
                
            // Register Cloudinary configuration
            builder.Services
                .Configure<CloudinarySettings>
                (builder.Configuration.GetSection("CloudinarySettings"));
                
            // Register Cloudinary service
            builder.Services
                .AddScoped<ICloudinaryService, CloudinaryService>();

            //Register Cloudinary URL service
            builder.Services
                .AddScoped<ICloudinaryUrlService, CloudinaryUrlService>();

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
                .AddScoped<IDropboxPictureService, DropboxPictureService>();
            builder.Services
                .AddScoped<IDropboxOAuthService, DropboxOAuthService>();
            builder.Services
                .AddScoped<IDropboxTokenProvider, DropboxTokenProvider>();
            builder.Services
                .AddScoped<ICoachDocumentUploadService, CoachDocumentUploadService>();
            
            // Token protector for encrypting Dropbox refresh tokens
            builder.Services.AddScoped<ITokenProtector, DataProtectionTokenProtector>();
            
            // Register authentication service
            builder.Services
                .AddScoped<IAuthenticationService, AuthenticationService>();

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

            // Register Admin services for coach verification and report review
            builder.Services
                .AddCoachVerificationServices()
                .AddReportReviewServices();

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
                // Use a single, consistent antiforgery configuration across the app
                options.HeaderName = "X-CSRF-TOKEN";               // what clients send in headers
                options.Cookie.Name = "CSRF-TOKEN";                // the antiforgery cookie name
                options.FormFieldName = "__RequestVerificationToken"; // hidden input name
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // dev-friendly
                options.Cookie.SameSite = SameSiteMode.Lax;        // allow normal navigation + XHR
                // HttpOnly default is true; we don't need JS to read this cookie
            });

            // Session for OAuth (store PKCE verifier/state; optionally tokens in dev)
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = ".ActioNator.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromHours(2);
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

            // Bind CloudinarySettings section from appsettings.json
            builder.Services.Configure<CloudinarySettings>(
                builder.Configuration.GetSection("CloudinarySettings"));

            // Register Cloudinary client as a singleton service for dependency injection
            builder.Services.AddSingleton(provider =>
            {
                // Resolve the configured CloudinarySettings instance
                CloudinarySettings config 
                = provider
                .GetRequiredService<IOptions<CloudinarySettings>>()
                .Value;

                // Create a new Cloudinary Account object using the configuration
                Account account 
                = new(config.CloudName, config.ApiKey, config.ApiSecret);

                // Instantiate and return the Cloudinary client with HTTPS enforced
                return new Cloudinary(account)
                {
                    Api = { Secure = true } // Ensures all URLs generated are HTTPS
                };
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
            
            // Enable session before auth so actions can read/write session
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            // Role-based alias routes to reuse User area controllers for Coach/Admin without duplication

            // Ensure genuine Coach area Home is not shadowed by alias
            app.MapControllerRoute(
                name: "coach_area_home",
                pattern: "Coach/Home/{action=Index}/{id?}",
                defaults: new { area = "Coach", controller = "Home" });

            // Coaches: access all other User-area controllers via Coach prefix (no regex)
            app.MapControllerRoute(
                name: "coach_user_alias",
                pattern: "Coach/{controller}/{action=Index}/{id?}",
                defaults: new { area = "User" })
               .WithMetadata(new SuppressLinkGenerationMetadata())
               .RequireAuthorization("CoachAreaAliasAccess");

            // Admins: explicit aliases for selected User-area controllers
            app.MapControllerRoute(
                name: "admin_user_alias_community",
                pattern: "Admin/Community/{action=Index}/{id?}",
                defaults: new { area = "User", controller = "Community" })
               .WithMetadata(new SuppressLinkGenerationMetadata())
               .RequireAuthorization("AdminAreaAliasAccess");

            app.MapControllerRoute(
                name: "admin_user_alias_userprofile",
                pattern: "Admin/UserProfile/{action=Index}/{id?}",
                defaults: new { area = "User", controller = "UserProfile" })
               .WithMetadata(new SuppressLinkGenerationMetadata())
               .RequireAuthorization("AdminAreaAliasAccess");

            // Map area route with lower priority than alias routes
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

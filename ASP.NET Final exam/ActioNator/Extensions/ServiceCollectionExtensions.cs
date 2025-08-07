using ActioNator.Services.Implementations.ReportVerificationService;
using ActioNator.Services.Implementations.VerifyCoach;
using ActioNator.Services.Interfaces.ReportVerificationService;
using ActioNator.Services.Interfaces.VerifyCoachServices;
using Microsoft.Extensions.DependencyInjection;

namespace ActioNator.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCoachVerificationServices(this IServiceCollection services)
        {
            services.AddScoped<ICoachVerificationService, CoachVerificationService>();
            return services;
        }
        
        public static IServiceCollection AddReportReviewServices(this IServiceCollection services)
        {
            services.AddScoped<IReportReviewService, ReportReviewService>();
            return services;
        }
    }
}

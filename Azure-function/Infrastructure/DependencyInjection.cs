
using Application.Common.Interfaces;
using Domain.Interfaces;
using Infrastructure.Azure;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure DbContext
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // User repository
            services.AddScoped<IUserRepository, UserRepository>();

            // JWT token service
            services.AddScoped<Domain.Interfaces.IJwtTokenService, Infrastructure.Services.JwtTokenService>();

            // Video processing pipeline
            services.AddScoped<IVideoProcessingJobRepository, VideoProcessingJobRepository>();
            services.AddTransient<IVideoTranscoderService, FFmpegTranscoderService>();
            services.AddScoped<IAzureBlobFolderUploadService, AzureBlobFolderUploadService>();
            services.AddHostedService<VideoProcessingBackgroundService>();

            // ── Email pipeline ──────────────────────────────────────────────────
            // SmtpEmailService is Transient because each call creates its own
            // MailKit SmtpClient — there is no shared mutable state to protect.
            services.AddTransient<IEmailService, SmtpEmailService>();
            services.AddScoped<IBlobStorageService, BlobStorageService>();

            // IEmailQueueRepository is Scoped (same lifetime as DbContext).
            // Retained for EmailQueueBackgroundService which polls and dispatches
            // emails that arrive on the Azure Storage Queue.
            services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();

            // Background worker that polls the EmailQueue table every 60 s.
            services.AddHostedService<EmailQueueBackgroundService>();

            // ── Azure Storage Queue ─────────────────────────────────────────────
            // AzureQueueService is Singleton: QueueClient is thread-safe by design
            // and the queue-existence guard is per-instance.  A single instance
            // is safe and avoids redundant CreateIfNotExists calls.
            services.AddSingleton<IAzureQueueService, AzureQueueService>();

            return services;
        }
    }
}
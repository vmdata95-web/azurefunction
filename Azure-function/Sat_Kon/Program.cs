using Domain.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Data;
using Infrastructure.Hubs;
using Infrastructure.Repositories;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sat_Kon.Middleware;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Application.Features.Users.Command.Userlogin;
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ──────────────────────────────────────────────────────────────────────────────
// DATABASE
// ──────────────────────────────────────────────────────────────────────────────
var connectionString = configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ──────────────────────────────────────────────────────────────────────────────
// REPOSITORIES & SERVICES
// ──────────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<ISpeakerRepository, SpeakerRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ILiveStreamRepository, LiveStreamRepository>();
builder.Services.AddScoped<IAgendaRepository, AgendaRepository>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IUserActivityLogRepository, UserActivityLogRepository>();
builder.Services.AddScoped<IExhibitorRepository, ExhibitorRepository>();
builder.Services.AddScoped<IRoomImageRepository, RoomImageRepository>();
builder.Services.AddScoped<IVideoJobRepository, VideoJobRepository>();

// Video Processing Pipeline
builder.Services.AddScoped<IVideoProcessingJobRepository, VideoProcessingJobRepository>();
builder.Services.AddTransient<IVideoTranscoderService, FFmpegTranscoderService>();
builder.Services.AddScoped<IAzureBlobFolderUploadService, AzureBlobFolderUploadService>();
//builder.Services.AddHostedService<VideoProcessingBackgroundService>();

// Azure Blob Storage — scoped matches the DbContext lifetime (one per HTTP request)
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// Pseudo-live HLS streaming & helpers
builder.Services.AddSingleton<Application.Common.Interfaces.ITimeProvider, Application.Common.Services.TimeProvider>();
builder.Services.AddSingleton<Application.Common.Interfaces.IPseudoLiveService, Application.Common.Services.PseudoLiveService>();
// Session validation service used by pseudo-live handlers
builder.Services.AddScoped<Application.Common.Interfaces.ISessionValidationService, Application.Common.Services.SessionValidationService>();

// IChatHubService — the Domain abstraction that all Application handlers use
// to broadcast SignalR events. Concrete implementation wraps IHubContext<ChatHub>.
builder.Services.AddScoped<IChatHubService, ChatHubService>();


builder.Services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
// SMTP service: scoped — creates a fresh MailKit SmtpClient per scope.
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
// Background service: polls the EmailQueue table every 60 seconds.
//builder.Services.AddHostedService<EmailQueueBackgroundService>();

// Azure Storage Queue — used by RegisterUserHandler to enqueue welcome emails.
// Singleton: QueueClient is thread-safe; the queue-existence guard is per-instance.
builder.Services.AddSingleton<Application.Common.Interfaces.IAzureQueueService, Infrastructure.Azure.AzureQueueService>();


// IChatHubService — the Domain abstraction that all Application handlers use
// to broadcast SignalR events. Concrete implementation wraps IHubContext<ChatHub>.
builder.Services.AddScoped<IChatHubService, ChatHubService>();

// ──────────────────────────────────────────────────────────────────────────────
// CONTROLLERS
// ──────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ──────────────────────────────────────────────────────────────────────────────
// SIGNALR
//
// AddSignalR() registers the hub dispatch infrastructure.
//
// Redis backplane (optional, required for multi-instance/load-balanced deployments):
//   Uncomment the AddStackExchangeRedis line and add "RedisConnection" to
//   appsettings.json. Without this, SignalR uses in-process memory, which means
//   clients on different server instances cannot exchange messages.
// ──────────────────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// OPTIONAL Redis backplane — uncomment for production scale-out:
// builder.Services.AddSignalR()
//     .AddStackExchangeRedis(
//         configuration.GetConnectionString("RedisConnection")!,
//         opts => { opts.Configuration.ChannelPrefix = "SatKon"; });

// ──────────────────────────────────────────────────────────────────────────────
// FLUENT VALIDATION + MEDIATR
// ──────────────────────────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<Application.AssemblyMarker>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.Configure<LoginSettings>(
    builder.Configuration.GetSection("LoginSettings"));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Application.AssemblyMarker).Assembly);
});

builder.Services.AddAutoMapper(typeof(Application.AssemblyMarker).Assembly);

// ──────────────────────────────────────────────────────────────────────────────
// HTTP CONTEXT ACCESSOR
// ──────────────────────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ──────────────────────────────────────────────────────────────────────────────
// CORS
//
// AllowCredentials() is REQUIRED for SignalR:
//   The browser's SignalR JavaScript client sends credentials (cookies or the
//   access_token query string) during the WebSocket upgrade handshake.
//   Without AllowCredentials() the browser blocks the connection.
//
// WithOrigins() must list the exact frontend origin — wildcard "*" is not
// allowed when AllowCredentials() is set.
// ──────────────────────────────────────────────────────────────────────────────
var allowedOrigins = configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();   // Required for SignalR
    });
});

// ──────────────────────────────────────────────────────────────────────────────
// JWT AUTHENTICATION
//
// Key change — OnMessageReceived event:
//   Standard browser WebSocket connections CANNOT send custom HTTP headers
//   (e.g. "Authorization: Bearer ...") during the initial HTTP upgrade handshake.
//   The SignalR JavaScript client works around this by appending the JWT token
//   as a URL query parameter: /chatHub?access_token=<token>
//
//   OnMessageReceived intercepts every incoming request to the JWT middleware
//   pipeline and, if the request is for the /chatHub path and carries an
//   access_token query parameter, it manually sets context.Token so that
//   the standard JwtBearer validation runs as normal.
//
//   All other validation parameters (Issuer, Audience, signing keys) are
//   kept exactly as they were.
// ──────────────────────────────────────────────────────────────────────────────
// JWT AUTHENTICATION
// ──────────────────────────────────────────────────────────────────────────────
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,

        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)
        ),

        NameClaimType = "sub",
        RoleClaimType = "role"
    };

    // Logging & Events
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken)
                && (path.StartsWithSegments("/chatHub") || path.StartsWithSegments("/streamHub")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "JWT Validation Failed. Path: {Path}. Reason: {Message}",
                context.HttpContext.Request.Path, context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var email = context.Principal?.FindFirst("email")?.Value
                        ?? context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;
            logger.LogInformation("JWT Validation Succeeded. Path: {Path}. Authenticated User Email: {Email}",
                context.HttpContext.Request.Path, email);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Unauthorized access attempt to: {Path}. Error: {Error}, Description: {Description}",
                context.HttpContext.Request.Path, context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // Policies using enum names to avoid string duplication
    options.AddPolicy("AdminOnly", p => p.RequireRole(Domain.Enums.UserRole.Admin.ToString()));
    options.AddPolicy("SuperAdminOnly", p => p.RequireRole(Domain.Enums.UserRole.SuperAdmin.ToString()));
    options.AddPolicy("SpeakerOnly", p => p.RequireRole(Domain.Enums.UserRole.Speaker.ToString()));
    options.AddPolicy("UserOnly", p => p.RequireRole(Domain.Enums.UserRole.User.ToString()));
    options.AddPolicy("AdminOrSuperAdmin", p => p.RequireRole(Domain.Enums.UserRole.Admin.ToString(), Domain.Enums.UserRole.SuperAdmin.ToString()));
});

// ──────────────────────────────────────────────────────────────────────────────
// SWAGGER / OPENAPI
// ──────────────────────────────────────────────────────────────────────────────
var swaggerEnabled = configuration.GetValue<bool>("Swagger:Enabled");

if (swaggerEnabled)
{
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Sat_Kon API",
            Version = "v1"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization. Enter your token WITHOUT the 'Bearer' prefix.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()   
            }
        });
    });
}
// ──────────────────────────────────────────────────────────────────────────────
// BUILD
// ──────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Register the global exception handling middleware first to intercept all pipeline errors
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseStaticFiles();

// ──────────────────────────────────────────────────────────────────────────────
// SWAGGER
// ──────────────────────────────────────────────────────────────────────────────
if (swaggerEnabled)
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sat_Kon API v1");
        c.RoutePrefix = "swagger";
    });
}

// ──────────────────────────────────────────────────────────────────────────────
// MIDDLEWARE PIPELINE ORDER
//
// Order is critical:
//   1. UseCors()         — must be before UseAuthentication for WebSocket upgrades
//   2. UseAuthentication — validates JWT (including from query string via event above)
//   3. UseAuthorization  — enforces [Authorize] on hub and controllers
//   4. UseMiddleware     — custom session middleware
//   5. MapHub            — registers the SignalR WebSocket endpoint
//   6. MapControllers    — registers REST API endpoints
// ──────────────────────────────────────────────────────────────────────────────
app.UseCors("AllowSpecificOrigins");
app.UseAuthentication();
app.UseMiddleware<SessionMiddleware>();
app.UseAuthorization();

// Map the SignalR hubs — ChatHub and StreamHub are in Infrastructure.Hubs namespace
app.MapHub<ChatHub>("/chatHub");
app.MapHub<StreamHub>("/streamHub");

app.MapControllers();

app.Run();

//app.Run("http://0.0.0.0:5087");
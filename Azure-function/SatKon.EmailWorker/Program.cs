using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ── Isolated Worker host entry point ─────────────────────────────────────────
//
// .NET 8 Isolated Worker model: the Function host and the user code run in
// separate processes.  The host communicates with this process via gRPC.
// All configuration, DI, and middleware are set up here in Program.cs.
// ─────────────────────────────────────────────────────────────────────────────

var host = new HostBuilder()

    // ── Azure Functions Isolated Worker defaults ──────────────────────────────
    .ConfigureFunctionsWorkerDefaults()

    // ── Application configuration ─────────────────────────────────────────────
    .ConfigureAppConfiguration((context, config) =>
    {
        // appsettings.json — base configuration (committed to source control).
        // IMPORTANT: the .csproj declares this as <Content CopyToOutputDirectory=Always>
        // so the file is guaranteed to be present next to the executable at runtime.
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        // ⚠ DO NOT load local.settings.json here.
        // The Azure Functions host reads local.settings.json itself and injects
        // the "Values" entries as flat environment variables into this worker process.
        // If we also load the file as JSON, every key gets nested under "Values:"
        // (e.g. "Values:AzureWebJobsStorage") which prevents the runtime from
        // resolving AzureWebJobsStorage — causing a ContainerNotFound 404 at startup.
        // AddEnvironmentVariables() below picks up all the host-injected values correctly.

        // Environment variables override file-based config.
        // On Azure this is where App Settings / Key Vault references are injected.
        config.AddEnvironmentVariables();
    })

    // ── Dependency injection ──────────────────────────────────────────────────
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // ── Logging ───────────────────────────────────────────────────────────
        // The Isolated Worker SDK registers Application Insights automatically
        // when APPLICATIONINSIGHTS_CONNECTION_STRING is present.
        // Console sink is registered here for local and stdout logging.
        services.AddLogging(logging =>
        {
            logging.AddConsole();
        });

        // ── Startup SMTP configuration guard ──────────────────────────────────
        // Runs as a hosted service before the first queue message is processed.
        // Logs every Email:* key so you can confirm appsettings.json is loaded.
        // Throws immediately if any required key is absent — no silent failures.
        services.AddHostedService<SmtpStartupDiagnosticsService>();

        // ── Email service ──────────────────────────────────────────────────────
        // SmtpEmailService reads its configuration from the "Email" section
        // of IConfiguration (host, port, credentials, TLS settings).
        // Transient because each invocation creates its own MailKit SmtpClient.
        services.AddTransient<IEmailService, SmtpEmailService>();

        // ── Session & Database Services ───────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        // ── Azure Web PubSub ──────────────────────────────────────────────────
        // WebPubSubService is Singleton: WebPubSubServiceClient is thread-safe
        // and constructing it on every invocation would be wasteful.
        // The service reads AzureWebPubSubConnectionString and
        // AzureWebPubSubHubName from IConfiguration at startup.
        services.AddSingleton<IWebPubSubService, WebPubSubService>();
    })

    .Build();

await host.RunAsync();


// ─────────────────────────────────────────────────────────────────────────────
// Startup diagnostics — verifies SMTP configuration is reachable before
// the first queue message is ever processed.
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class SmtpStartupDiagnosticsService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpStartupDiagnosticsService> _logger;

    public SmtpStartupDiagnosticsService(
        IConfiguration configuration,
        ILogger<SmtpStartupDiagnosticsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("──────────────────────────────────────────────────────────");
        _logger.LogInformation("[StartupDiagnostics] Verifying SMTP configuration…");
        _logger.LogInformation("──────────────────────────────────────────────────────────");

        // ── Read every key SmtpEmailService will use ──────────────────────────
        var smtpHost     = _configuration["Email:SmtpHost"];
        var smtpPortRaw  = _configuration["Email:SmtpPort"];
        var useSslRaw    = _configuration["Email:UseSsl"];
        var senderEmail  = _configuration["Email:SenderEmail"];
        var senderName   = _configuration["Email:SenderName"];
        var username     = _configuration["Email:Username"];
        var password     = _configuration["Email:Password"];   // checked but NOT logged
        var acceptAnyCert = _configuration["Email:AcceptAnyServerCertificate"];
        var maxRetry     = _configuration["Email:MaxRetryAttempts"];

        // ── Log all non-secret values ─────────────────────────────────────────
        _logger.LogInformation("[StartupDiagnostics] Email:SmtpHost              = {Value}", smtpHost     ?? "(null — NOT LOADED)");
        _logger.LogInformation("[StartupDiagnostics] Email:SmtpPort              = {Value}", smtpPortRaw  ?? "(null — NOT LOADED)");
        _logger.LogInformation("[StartupDiagnostics] Email:UseSsl                = {Value}", useSslRaw    ?? "(null — NOT LOADED)");
        _logger.LogInformation("[StartupDiagnostics] Email:SenderEmail           = {Value}", senderEmail  ?? "(null — NOT LOADED)");
        _logger.LogInformation("[StartupDiagnostics] Email:SenderName            = {Value}", senderName   ?? "(null — NOT LOADED)");
        _logger.LogInformation("[StartupDiagnostics] Email:Username              = {Value}", username     ?? "(null — NOT LOADED)");
        _logger.LogInformation("[StartupDiagnostics] Email:Password Loaded       = {Value}", !string.IsNullOrWhiteSpace(password));
        _logger.LogInformation("[StartupDiagnostics] Email:AcceptAnyServerCert   = {Value}", acceptAnyCert ?? "(null — using default: false)");
        _logger.LogInformation("[StartupDiagnostics] Email:MaxRetryAttempts      = {Value}", maxRetry      ?? "(null — using default: 3)");
        _logger.LogInformation("──────────────────────────────────────────────────────────");

        // ── Fail fast if any required key is missing ──────────────────────────
        // "Required" means SmtpEmailService will throw from RequiredConfig() anyway —
        // we surface the error here at startup rather than on the first message.
        var missingKeys = new List<string>();

        if (string.IsNullOrWhiteSpace(smtpHost))   missingKeys.Add("Email:SmtpHost");
        if (string.IsNullOrWhiteSpace(username))    missingKeys.Add("Email:Username");
        if (string.IsNullOrWhiteSpace(password))    missingKeys.Add("Email:Password");
        if (string.IsNullOrWhiteSpace(senderEmail)) missingKeys.Add("Email:SenderEmail");

        if (missingKeys.Count > 0)
        {
            var missing = string.Join(", ", missingKeys);
            _logger.LogCritical(
                "[StartupDiagnostics] ✗ SMTP configuration is INCOMPLETE. " +
                "The following required keys are null or empty: {MissingKeys}. " +
                "Verify that appsettings.json is present in the output directory " +
                "and that the key names match exactly (case-sensitive).",
                missing);

            throw new InvalidOperationException(
                $"Azure Function cannot start: required SMTP configuration key(s) are missing: {missing}. " +
                $"Check that appsettings.json is copied to the output directory " +
                $"(Build Action = Content, Copy To Output Directory = Always).");
        }

        _logger.LogInformation(
            "[StartupDiagnostics] ✓ All required SMTP configuration keys are present. " +
            "Host={Host}:{Port} Username={Username}",
            smtpHost, smtpPortRaw, username);

        return Task.CompletedTask;
    }
    // stop

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

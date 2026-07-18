using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    /// <summary>
    /// Hosted background service that processes the EmailQueue table.
    ///
    /// Design:
    ///   • Uses <see cref="IServiceScopeFactory"/> (same pattern as
    ///     <see cref="VideoProcessingBackgroundService"/>) so that scoped services
    ///     (DbContext, repositories) are resolved fresh per tick — never shared across ticks.
    ///   • Polls every 60 seconds.
    ///   • On startup: resets any records stuck in Processing → Pending (crash recovery).
    ///   • Registration API response is NOT delayed — the handler only enqueues a row;
    ///     actual SMTP delivery happens here, asynchronously.
    ///
    /// Lifecycle per email:
    ///   Pending → Processing → Sent       (happy path)
    ///   Pending → Processing → Failed     (SMTP error)
    ///   Processing → Pending              (startup reset after crash)
    /// </summary>
    public sealed class EmailQueueBackgroundService : BackgroundService
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(1);
        private const int BatchSize = 50;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailQueueBackgroundService> _logger;

        public EmailQueueBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<EmailQueueBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // ── Entry point ───────────────────────────────────────────────────────

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[EmailQueue] Background service starting.");

            // Recover rows stuck in Processing from a prior crash/shutdown.
            await ResetStuckProcessingEmailsAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingEmailsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Never let the loop die — log and continue to the next tick.
                    _logger.LogError(ex, "[EmailQueue] Critical error in processing loop.");
                }

                try
                {
                    await Task.Delay(PollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break; // Clean shutdown.
                }
            }

            _logger.LogInformation("[EmailQueue] Background service stopping.");
        }

        // ── Fetch + dispatch batch ────────────────────────────────────────────

        private async Task ProcessPendingEmailsAsync(CancellationToken stoppingToken)
        {
            List<EmailQueue> pendingEmails;

            // Scope 1: fetch pending rows.
            using (var fetchScope = _scopeFactory.CreateScope())
            {
                var repo = fetchScope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();
                pendingEmails = await repo.GetPendingEmailsAsync(BatchSize, stoppingToken);
            }

            if (pendingEmails.Count == 0)
            {
                _logger.LogDebug("[EmailQueue] No pending emails found.");
                return;
            }

            _logger.LogInformation(
                "[EmailQueue] Found {Count} pending email(s) to process.", pendingEmails.Count);

            foreach (var email in pendingEmails)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                // Each email is processed in its own scope so a failure in one
                // does not affect the DbContext state for the others.
                await ProcessSingleEmailAsync(email, stoppingToken);
            }
        }

        // ── Send a single email ───────────────────────────────────────────────

        /// <summary>
        /// Attempts to send <paramref name="email"/> via SMTP.
        /// All state transitions (Processing → Sent/Failed) use a dedicated
        /// DI scope so there is no shared DbContext between emails.
        /// </summary>
        private async Task ProcessSingleEmailAsync(
            EmailQueue email,
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[EmailQueue] Processing email Id={Id}, To={To}, Subject={Subject}",
                email.Id, email.Email, email.Subject);

            // Scope 2: mark Processing + send + mark Sent/Failed.
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                // Step 1 — guard against double-processing in multi-instance deployments.
                await repo.MarkProcessingAsync(email.Id, stoppingToken);

                // Step 2 — send via SMTP.
                await emailService.SendAsync(
                    email.Email,
                    email.Subject,
                    email.Body,
                    email.IsHtml,
                    stoppingToken);

                // Step 3 — mark Sent.
                // Use CancellationToken.None: we must persist success even if host
                // begins shutting down between send completion and DB update.
                await repo.MarkSentAsync(email.Id, CancellationToken.None);

                _logger.LogInformation(
                    "[EmailQueue] Email Id={Id} sent successfully to {To}.",
                    email.Id, email.Email);
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down mid-send.  Leave record in Processing;
                // ResetStuckProcessingAsync on next startup will set it back to Pending.
                _logger.LogWarning(
                    "[EmailQueue] Send cancelled for Id={Id} due to host shutdown. " +
                    "Record will be reset to Pending on next startup.", email.Id);
                throw; // Propagate so the outer while-loop exits cleanly.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[EmailQueue] Failed to send email Id={Id} to {To}. Marking as Failed.",
                    email.Id, email.Email);

                // Use a fresh scope + CancellationToken.None so that the failure
                // is persisted even if the original scope's DbContext is in a bad state.
                try
                {
                    using var failScope = _scopeFactory.CreateScope();
                    var failRepo = failScope.ServiceProvider
                        .GetRequiredService<IEmailQueueRepository>();

                    await failRepo.MarkFailedAsync(email.Id, ex.Message, CancellationToken.None);

                    _logger.LogWarning(
                        "[EmailQueue] Email Id={Id} marked as Failed. RetryCount incremented.",
                        email.Id);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx,
                        "[EmailQueue] Failed to persist failure state for email Id={Id}.", email.Id);
                }
            }
        }

        // ── Startup recovery ──────────────────────────────────────────────────

        private async Task ResetStuckProcessingEmailsAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation(
                    "[EmailQueue] Checking for stuck 'Processing' emails from prior shutdown...");

                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();
                await repo.ResetStuckProcessingAsync(stoppingToken);

                _logger.LogInformation("[EmailQueue] Startup recovery complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[EmailQueue] Failed to reset stuck Processing emails on startup.");
            }
        }
    }

}

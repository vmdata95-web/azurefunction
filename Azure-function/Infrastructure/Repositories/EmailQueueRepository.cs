using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="IEmailQueueRepository"/>.
    ///
    /// Uses <see cref="AppDbContext"/> directly (scoped lifetime) so every method
    /// participates in the same unit-of-work as the caller.  The background service
    /// creates its own scope per tick to ensure DbContext is not shared across threads.
    /// </summary>
    public class EmailQueueRepository : IEmailQueueRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmailQueueRepository> _logger;

        public EmailQueueRepository(
            AppDbContext context,
            ILogger<EmailQueueRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task AddAsync(EmailQueue emailQueue, CancellationToken cancellationToken = default)
        {
            await _context.EmailQueues.AddAsync(emailQueue, cancellationToken);
            _logger.LogDebug(
                "[EmailQueue] Queued email for {Email} (Id={Id})",
                emailQueue.Email, emailQueue.Id);
        }

        /// <inheritdoc/>
        public async Task<List<EmailQueue>> GetPendingEmailsAsync(
            int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            return await _context.EmailQueues
                .Where(e => e.Status == (int)EmailStatus.Pending)
                .OrderBy(e => e.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var record = await FindOrThrowAsync(id, cancellationToken);
            record.Status = (int)EmailStatus.Processing;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("[EmailQueue] Id={Id} → Processing", id);
        }

        /// <inheritdoc/>
        public async Task MarkSentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var record = await FindOrThrowAsync(id, cancellationToken);
            record.Status = (int)EmailStatus.Sent;
            record.SentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("[EmailQueue] Id={Id} → Sent at {SentAt}", id, record.SentAt);
        }

        /// <inheritdoc/>
        public async Task MarkFailedAsync(
            Guid id,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            var record = await FindOrThrowAsync(id, cancellationToken);
            record.Status = (int)EmailStatus.Failed;
            record.RetryCount += 1;
            record.ErrorMessage = errorMessage.Length > 2000
                ? errorMessage[..2000]  // Guard against DB column limit
                : errorMessage;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug(
                "[EmailQueue] Id={Id} → Failed. RetryCount={Retry}. Error={Error}",
                id, record.RetryCount, errorMessage);
        }

        /// <inheritdoc/>
        public async Task ResetStuckProcessingAsync(CancellationToken cancellationToken = default)
        {
            var stuckRecords = await _context.EmailQueues
                .Where(e => e.Status == (int)EmailStatus.Processing)
                .ToListAsync(cancellationToken);

            if (stuckRecords.Count == 0) return;

            foreach (var record in stuckRecords)
                record.Status = (int)EmailStatus.Pending;

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning(
                "[EmailQueue] Reset {Count} stuck 'Processing' records back to 'Pending'.",
                stuckRecords.Count);
        }

        /// <inheritdoc/>
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        // ── Private helpers ──────────────────────────────────────────────────

        private async Task<EmailQueue> FindOrThrowAsync(Guid id, CancellationToken ct)
        {
            var record = await _context.EmailQueues.FindAsync(new object[] { id }, ct);
            if (record == null)
                throw new InvalidOperationException(
                    $"[EmailQueue] Record with Id={id} not found in database.");
            return record;
        }
    }

}

using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    /// <summary>
    /// Repository abstraction for the EmailQueue table.
    /// All database access goes through this interface so that Application
    /// handlers and background services remain free of EF Core dependencies.
    /// </summary>
    public interface IEmailQueueRepository
    {
        /// <summary>
        /// Inserts a new EmailQueue record with Status = Pending (0).
        /// Does NOT call SaveChanges — caller is responsible for committing.
        /// </summary>
        Task AddAsync(EmailQueue emailQueue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches all rows with Status = Pending (0), ordered by CreatedAt ascending
        /// (oldest first), limited to <paramref name="batchSize"/> rows to avoid
        /// memory spikes when the queue is large.
        /// </summary>
        Task<List<EmailQueue>> GetPendingEmailsAsync(
            int batchSize = 50,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions the record to Status = Processing (1).
        /// Called before attempting to send so that a second service instance
        /// does not pick up the same record concurrently.
        /// </summary>
        Task MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions the record to Status = Sent (2) and stamps SentAt.
        /// </summary>
        Task MarkSentAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions the record to Status = Failed (3), increments RetryCount,
        /// and persists the error message for diagnostics.
        /// </summary>
        Task MarkFailedAsync(
            Guid id,
            string errorMessage,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// On startup: resets any records stuck in Status = Processing (1) back to
        /// Pending (0).  This handles the case where the host was killed while an
        /// email was being sent and the record was never updated.
        /// </summary>
        Task ResetStuckProcessingAsync(CancellationToken cancellationToken = default);

        /// <summary>Persists all pending EF Core change-tracker changes.</summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }

}

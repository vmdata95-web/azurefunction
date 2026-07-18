using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Entities
{
        /// <summary>
        /// Represents a queued outbound email.
        /// The background service polls this table, sends the email,
        /// and updates status/retry information in place.
        ///
        /// Status values (stored as int):
        ///   0 = Pending   — ready to be picked up
        ///   1 = Processing — currently being sent (guard against double-processing)
        ///   2 = Sent       — delivery confirmed
        ///   3 = Failed     — all attempts exhausted or unrecoverable error
        /// </summary>
        public class EmailQueue
        {
            public Guid Id { get; set; } = Guid.NewGuid();

            /// <summary>
            /// The user this email belongs to.  Nullable — some system emails
            /// (e.g., admin alerts) may not have an associated user.
            /// </summary>
            public Guid? UserId { get; set; }

            public string Email { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;

            /// <summary>True when Body contains HTML markup.</summary>
            public bool IsHtml { get; set; } = true;

            /// <summary>
            /// 0 = Pending, 1 = Processing, 2 = Sent, 3 = Failed
            /// </summary>
            public int Status { get; set; } = 0;

            /// <summary>How many send attempts have been made (including failures).</summary>
            public int RetryCount { get; set; } = 0;

            /// <summary>Last error message from the SMTP layer, if any.</summary>
            public string? ErrorMessage { get; set; }

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            /// <summary>Set by the background service when delivery succeeds.</summary>
            public DateTime? SentAt { get; set; }
        }
    }


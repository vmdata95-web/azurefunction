using Application.DTOs;
using Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SatKon.EmailWorker.Functions
{
    /// <summary>
    /// Azure Queue Trigger function that processes welcome emails enqueued by
    /// <c>RegisterUserHandler</c> in the main API.
    ///
    /// ─────────────────────────────────────────────────────────────────────────
    /// Flow
    /// ─────────────────────────────────────────────────────────────────────────
    ///   1. Azure Storage Queue ("emails") receives a new message.
    ///   2. The Azure Functions runtime deserialises the raw string payload.
    ///   3. This function deserialises the JSON into <see cref="EmailQueueMessage"/>.
    ///   4. <see cref="IEmailService.SendAsync"/> is called with the message data.
    ///   5. On success  → function returns normally; the runtime removes the
    ///      message from the queue (mark complete).
    ///   6. On failure  → the exception is re-thrown; the runtime increments the
    ///      dequeue count.  After maxDequeueCount (configured in host.json) the
    ///      message is moved to the poison queue ("emails-poison") automatically
    ///      by the Azure Storage Queue SDK.
    ///
    /// No custom retry logic is implemented here — retry and dead-letter
    /// behaviour are delegated entirely to the Azure Functions / Storage Queue
    /// infrastructure.
    /// ─────────────────────────────────────────────────────────────────────────
    /// </summary>
    public sealed class EmailQueueFunction
    {
        // ── JSON options (shared, thread-safe) ───────────────────────────────
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true   // tolerant deserialisation
        };

        // ── Dependencies ─────────────────────────────────────────────────────
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailQueueFunction> _logger;

        // ── Constructor ──────────────────────────────────────────────────────
        public EmailQueueFunction(
            IEmailService emailService,
            ILogger<EmailQueueFunction> logger)
        {
            _emailService = emailService
                ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── Function entry point ─────────────────────────────────────────────

        /// <summary>
        /// Triggered whenever a new message arrives on the <c>emails</c> Azure
        /// Storage Queue.
        /// </summary>
        /// <param name="messagePayload">
        ///   Raw queue message string (JSON, Base64-decoded by the binding).
        /// </param>
        /// <param name="cancellationToken">
        ///   Propagated from the Azure Functions host; signalled on graceful shutdown.
        /// </param>
        [Function(nameof(EmailQueueFunction))]
        public async Task Run(
            [QueueTrigger(
                queueName: "emails",
                Connection = "AzureWebJobsStorage")]   // ← must be a flat env-var key / local.settings.json "Values" key,
                                                        //   NOT a colon-separated IConfiguration path.
                                                        //   "AzureWebJobsStorage" is defined in local.settings.json and
                                                        //   is the standard connection key for Azure Functions Storage.
            string messagePayload,
            CancellationToken cancellationToken)
        {
            // ── Step 1: Log trigger ──────────────────────────────────────────
            _logger.LogInformation(
                "[EmailQueueFunction] Queue trigger started. PayloadLength={Length}",
                messagePayload?.Length ?? 0);

            // ── Step 2: Deserialise ──────────────────────────────────────────
            EmailQueueMessage message;

            try
            {
                message = JsonSerializer.Deserialize<EmailQueueMessage>(
                    messagePayload!, _jsonOptions)
                    ?? throw new InvalidOperationException(
                        "Deserialised EmailQueueMessage is null — payload may be empty or malformed.");
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                // A permanently malformed message must NOT be retried endlessly.
                // Log as critical and re-throw so the runtime moves it to the
                // poison queue after maxDequeueCount exhaustion.
                _logger.LogCritical(ex,
                    "[EmailQueueFunction] Failed to deserialise queue message. " +
                    "Payload will be moved to poison queue after retry exhaustion. " +
                    "RawPayload={Payload}",
                    messagePayload);

                throw; // let the runtime handle retry / poison
            }

            // ── Step 3: Log message metadata ─────────────────────────────────
            _logger.LogInformation(
                "[EmailQueueFunction] Processing email. " +
                "UserId={UserId} To={Email} Subject={Subject} CreatedAt={CreatedAt}",
                message.UserId,
                message.Email,
                message.Subject,
                message.CreatedAt);

            // ── Step 4: Send email ────────────────────────────────────────────
            try
            {
                await _emailService.SendAsync(
                    toEmail: message.Email,
                    subject: message.Subject,
                    body: message.Body,
                    isHtml: message.IsHtml,
                    cancellationToken: cancellationToken);

                // ── Step 5: Log success ───────────────────────────────────────
                _logger.LogInformation(
                    "[EmailQueueFunction] Email sent successfully. " +
                    "UserId={UserId} To={Email} Subject={Subject}",
                    message.UserId,
                    message.Email,
                    message.Subject);

                // Function returns normally → runtime completes (deletes) the
                // message from the queue.
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down — do NOT mark the message as failed.
                // Re-throw so the runtime re-enqueues it for the next host instance.
                _logger.LogWarning(
                    "[EmailQueueFunction] Function cancelled during email send. " +
                    "UserId={UserId} To={Email} — message will be retried.",
                    message.UserId,
                    message.Email);

                throw;
            }
            catch (Exception ex)
            {
                // ── Step 6: Log failure and re-throw for runtime retry ────────
                _logger.LogError(ex,
                    "[EmailQueueFunction] Email sending FAILED. " +
                    "UserId={UserId} To={Email} Subject={Subject} " +
                    "Error={ErrorType}: {ErrorMessage}",
                    message.UserId,
                    message.Email,
                    message.Subject,
                    ex.GetType().Name,
                    ex.Message);

                // Re-throw so Azure Functions increments the dequeue count.
                // After maxDequeueCount attempts the message is automatically
                // moved to the "emails-poison" queue by the runtime.
                throw;
            }
        }
    }
}

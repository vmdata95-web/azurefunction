using Application.Common.Interfaces;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Azure
{
    /// <summary>
    /// Production implementation of <see cref="IAzureQueueService"/> backed by
    /// Azure Storage Queues (Azure.Storage.Queues SDK).
    ///
    /// Responsibilities:
    ///   • Reads connection string and queue name from <c>AzureStorage</c>
    ///     configuration section.
    ///   • Lazily creates the queue if it does not exist (<c>CreateIfNotExistsAsync</c>).
    ///   • Serialises the message payload to JSON using <see cref="System.Text.Json"/>.
    ///   • Base64-encodes the JSON string before sending — required when the
    ///     Azure Queue SDK is configured with <see cref="QueueMessageEncoding.Base64"/>
    ///     (the SDK default as of v12).
    ///   • Fully async; never blocks the calling thread.
    ///   • Structured logging via <see cref="ILogger{TCategoryName}"/>.
    /// </summary>
    public sealed class AzureQueueService : IAzureQueueService
    {
        // ── Configuration keys ───────────────────────────────────────────────
        private const string SectionName = "AzureStorage";
        private const string ConnectionStringKey = "ConnectionString";
        private const string QueueNameKey = "QueueName";

        // ── JSON serialisation options (shared, thread-safe) ─────────────────
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // ── Fields ───────────────────────────────────────────────────────────
        private readonly QueueClient _queueClient;
        private readonly ILogger<AzureQueueService> _logger;

        // Tracks whether the queue has been confirmed to exist for this
        // service instance lifetime — avoids a redundant SDK round-trip on
        // every EnqueueAsync call after the first successful one.
        private bool _queueEnsured;

        // ── Constructor ──────────────────────────────────────────────────────

        /// <summary>
        /// Resolves configuration values and constructs a <see cref="QueueClient"/>
        /// with <see cref="QueueMessageEncoding.Base64"/> so that the SDK
        /// automatically Base64-encodes outgoing messages.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <c>AzureStorage:ConnectionString</c> or
        /// <c>AzureStorage:QueueName</c> is missing or empty.
        /// </exception>
        public AzureQueueService(
            IConfiguration configuration,
            ILogger<AzureQueueService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var section = configuration.GetSection(SectionName);

            var connectionString = section[ConnectionStringKey];
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Configuration key '{SectionName}:{ConnectionStringKey}' is required but was not found or is empty.");

            var queueName = section[QueueNameKey];
            if (string.IsNullOrWhiteSpace(queueName))
                throw new InvalidOperationException(
                    $"Configuration key '{SectionName}:{QueueNameKey}' is required but was not found or is empty.");

            // QueueMessageEncoding.Base64 instructs the SDK to Base64-encode
            // every outgoing message body automatically.  This is the standard
            // setting expected by most Azure Queue consumers and Azure Functions.
            var options = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };

            _queueClient = new QueueClient(connectionString, queueName, options);

            _logger.LogInformation(
                "[AzureQueueService] Initialised. Queue={QueueName}",
                queueName);
        }

        // ── IAzureQueueService ───────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task EnqueueAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                // Ensure the queue exists exactly once per service instance.
                await EnsureQueueExistsAsync(cancellationToken).ConfigureAwait(false);

                // Serialise to compact JSON.
                var json = JsonSerializer.Serialize(message, _jsonOptions);

                // The SDK handles Base64 encoding internally because we
                // configured QueueMessageEncoding.Base64 in the constructor.
                // We pass the raw UTF-8 JSON string directly.
                var receipt = await _queueClient
                    .SendMessageAsync(json, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "[AzureQueueService] Message enqueued successfully. " +
                    "Queue={QueueName} MessageId={MessageId} Type={MessageType}",
                    _queueClient.Name,
                    receipt.Value.MessageId,
                    typeof(T).Name);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "[AzureQueueService] EnqueueAsync was cancelled. Queue={QueueName} Type={MessageType}",
                    _queueClient.Name,
                    typeof(T).Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AzureQueueService] Failed to enqueue message. " +
                    "Queue={QueueName} Type={MessageType}",
                    _queueClient.Name,
                    typeof(T).Name);
                throw;
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Creates the Azure Storage Queue if it does not already exist.
        /// After the first successful call the result is cached so subsequent
        /// calls return immediately without making a network request.
        /// </summary>
        private async Task EnsureQueueExistsAsync(CancellationToken cancellationToken)
        {
            if (_queueEnsured) return;

            await _queueClient
                .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _queueEnsured = true;

            _logger.LogDebug(
                "[AzureQueueService] Queue existence confirmed. Queue={QueueName}",
                _queueClient.Name);
        }
    }
}

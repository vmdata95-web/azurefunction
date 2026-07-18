using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
//using SatKon.EmailWorker.Services;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SatKon.EmailWorker.Functions
{
    /// <summary>
    /// Service Bus Trigger Function that processes session status updates.
    /// It listens to the "session-status-queue" queue.
    /// It handles "Live" and "Ended" actions to update session status in the database.
    /// Idempotency checks ensure that status updates are only performed when necessary.
    /// </summary>
    public sealed class SessionStatusQueueFunction
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ISessionRepository _sessionRepository;
        private readonly ILogger<SessionStatusQueueFunction> _logger;
        private readonly IWebPubSubService _webPubSubService;

        public SessionStatusQueueFunction(
            ISessionRepository sessionRepository,
            ILogger<SessionStatusQueueFunction> logger,
            IWebPubSubService webPubSubService)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webPubSubService = webPubSubService ?? throw new ArgumentNullException(nameof(webPubSubService));
        }
        [Function(nameof(SessionStatusQueueFunction))]
        public async Task Run(
    [ServiceBusTrigger(
        queueName: "session-status-queue",
        Connection = "AzureServiceBusConnectionString")]
    string messagePayload,
    int deliveryCount,
    CancellationToken cancellationToken)
        {
            // ── Step 1: Log Function Started & Message Received ────────────────────
            _logger.LogInformation("[SessionStatusQueueFunction] Function execution started.");
            _logger.LogInformation("[SessionStatusQueueFunction] Message received. PayloadLength={0}", messagePayload?.Length ?? 0);

            // Log if this is a retry attempt
            if (deliveryCount > 1)
            {
                _logger.LogWarning("[SessionStatusQueueFunction] Retry detected. DeliveryCount={0}", deliveryCount);
            }

            // ── Step 2: Deserialise DTO ──────────────────────────────────────────
            SessionStatusMessage message;
            try
            {
                message = JsonSerializer.Deserialize<SessionStatusMessage>(messagePayload!, _jsonOptions)
                    ?? throw new InvalidOperationException("Deserialised SessionStatusMessage is null.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "[SessionStatusQueueFunction] Failed to deserialize queue message. " +
                    "Re-throwing exception to trigger host retry/dead-letter. " +
                    "RawPayload={0}",
                    messagePayload);
                throw;
            }

            // ── Step 3: Extract SessionId & Action ───────────────────────────────
            var sessionId = message.SessionId;
            var action = message.Action;

            _logger.LogInformation(
                "[SessionStatusQueueFunction] Processing status transition. SessionId={0}, Action={1}",
                sessionId, action);

            if (sessionId == Guid.Empty)
            {
                _logger.LogWarning("[SessionStatusQueueFunction] Received empty SessionId. Skipping message.");
                return;
            }

            // ── Step 4: Perform Database Update & Idempotency Logic ───────────────
            try
            {
                var session = await _sessionRepository.GetByIdForUpdateAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    _logger.LogWarning(
                        "[SessionStatusQueueFunction] Session with ID {0} not found in database. " +
                        "This could be due to replication lag. Triggering retry.",
                        sessionId);
                    throw new InvalidOperationException($"Session {sessionId} not found.");
                }

                const string StatusLive = "Live";
                const string StatusEnded = "Ended";

                bool isUpdated = false;
                var previousStatus = session.Status;

                if (string.Equals(action, StatusLive, StringComparison.OrdinalIgnoreCase))
                {
                    // Idempotency: If already Live or Ended, ignore duplicate Live messages.
                    if (string.Equals(session.Status, StatusLive, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(session.Status, StatusEnded, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation(
                            "[SessionStatusQueueFunction] Session {0} is already in state '{1}'. Ignoring duplicate Live message.",
                            session.Id, session.Status);
                    }
                    else
                    {
                        session.Status = StatusLive;
                        isUpdated = true;
                        _logger.LogInformation(
                            "[SessionStatusQueueFunction] Transitioning Session {0} status: '{1}' → '{2}'.",
                            session.Id, previousStatus, StatusLive);
                    }
                }
                else if (string.Equals(action, StatusEnded, StringComparison.OrdinalIgnoreCase))
                {
                    // Idempotency: If already Ended, ignore duplicate Ended messages.
                    if (string.Equals(session.Status, StatusEnded, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation(
                            "[SessionStatusQueueFunction] Session {0} is already in state '{1}'. Ignoring duplicate Ended message.",
                            session.Id, session.Status);
                    }
                    else
                    {
                        session.Status = StatusEnded;
                        isUpdated = true;
                        _logger.LogInformation(
                            "[SessionStatusQueueFunction] Transitioning Session {0} status: '{1}' → '{2}'.",
                            session.Id, previousStatus, StatusEnded);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "[SessionStatusQueueFunction] Unknown action value '{0}' received for Session {1}.",
                        action, session.Id);
                }

                if (isUpdated)
                {
                    // SaveChanges — commit the status transition to the database.
                    // Web PubSub is published ONLY after this succeeds.
                    await _sessionRepository.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "[SessionStatusQueueFunction] Database update success. SessionId={0}, Action={1}, Status transitioned from '{2}' to '{3}'",
                        session.Id, action, previousStatus, session.Status);

                    // Notify connected clients in room-{RoomId} via Web PubSub.
                    // Failure here does NOT throw — the database update is already
                    // committed and must not be rolled back by a notification error.
                    await PublishStatusChangedAsync(session, cancellationToken);
                }
                else
                {
                    _logger.LogInformation(
                        "[SessionStatusQueueFunction] Idempotent flow. No database update required for SessionId={0}.",
                        session.Id);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "[SessionStatusQueueFunction] Execution was cancelled. SessionId={0}, Action={1}",
                    sessionId, action);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SessionStatusQueueFunction] Database update failure. " +
                    "SessionId={0}, Action={1}, Attempt={2}. " +
                    "If this failure persists across attempts, the message will be dead-lettered. " +
                    "Error={3}: {4}",
                    sessionId, action, deliveryCount, ex.GetType().Name, ex.Message);

                throw; // Rethrow to allow Service Bus retry / dead-lettering behavior
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Publishes a <c>SESSION_STATUS_CHANGED</c> Web PubSub event to the
        /// group <c>room-{RoomId}</c> after the database has been updated.
        /// <para>
        /// This method MUST only be called after <c>SaveChangesAsync</c>
        /// completes without throwing.  It delegates exception handling entirely
        /// to <see cref="WebPubSubService"/> (which logs and swallows), so the
        /// caller never sees a notification failure turn into a Service Bus retry.
        /// </para>
        /// </summary>
        private async Task PublishStatusChangedAsync(
    Session session,
    CancellationToken cancellationToken)
        {
            await _webPubSubService.SendSessionStatusChangedAsync(
    roomId: session.RoomId,
    sessionId: session.Id,
    title: session.Title,
    status: session.Status,
    cancellationToken: cancellationToken);
        }
    }
}


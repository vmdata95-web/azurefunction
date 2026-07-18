using Azure.Core;
using Azure.Messaging.WebPubSub;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public sealed class WebPubSubService : IWebPubSubService
    {
        private const string ConnectionStringConfigKey = "AzureWebPubSubConnectionString";
        private const string HubNameConfigKey = "AzureWebPubSubHubName";

        private const string GroupPrefix = "room-";
        private const string ChatGroupPrefix = "session-";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly WebPubSubServiceClient _client;
        private readonly ILogger<WebPubSubService> _logger;
        private readonly string _hubName;

        public WebPubSubService(
            IConfiguration configuration,
            ILogger<WebPubSubService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var connectionString = configuration[ConnectionStringConfigKey];
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"[WebPubSubService] Required configuration key '{ConnectionStringConfigKey}' is missing or empty.");

            _hubName = configuration[HubNameConfigKey] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_hubName))
                throw new InvalidOperationException(
                    $"[WebPubSubService] Required configuration key '{HubNameConfigKey}' is missing or empty.");

            _client = new WebPubSubServiceClient(connectionString, _hubName);

            _logger.LogInformation(
                "[WebPubSubService] Initialised. Hub={HubName}",
                _hubName);
        }

        public async Task SendSessionStatusChangedAsync(
            Guid roomId,
            Guid sessionId,
            string title,
            string status,
            CancellationToken cancellationToken = default)
        {
            var groupName = $"{GroupPrefix}{roomId}";

            _logger.LogInformation(
                "[WebPubSubService] WebPubSub message publishing started. Hub={Hub} Group={Group} SessionId={SessionId} Status={Status}",
                _hubName, groupName, sessionId, status);

            try
            {
                var payload = new SessionStatusChangedPayload(
                    Type: "SESSION_STATUS_CHANGED",
                    SessionId: sessionId,
                    Title: title,
                    Status: status
                );
                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                var requestContent = RequestContent.Create(BinaryData.FromString(json));
                var requestContext = new global::Azure.RequestContext { CancellationToken = cancellationToken };

                await _client.SendToGroupAsync(
                    group: groupName,
                    content: requestContent,
                    contentType: ContentType.ApplicationJson,
                    excluded: null,
                    context: requestContext)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WebPubSubService] Session Status Publishing FAILED. Hub={Hub} Group={Group} SessionId={SessionId} Status={Status}",
                    _hubName, groupName, sessionId, status);
            }
        }

        public async Task SendToUserAsync(
            Guid userId,
            string eventName,
            object payload,
            CancellationToken cancellationToken = default)
        {
            var userStr = userId.ToString();
            _logger.LogInformation(
                "[WebPubSubService] SendToUser started. Hub={Hub} UserId={UserId} Event={Event}",
                _hubName, userStr, eventName);

            try
            {
                var wrapped = new { type = eventName, data = payload };
                var json = JsonSerializer.Serialize(wrapped, _jsonOptions);
                var requestContent = RequestContent.Create(BinaryData.FromString(json));
                var requestContext = new global::Azure.RequestContext { CancellationToken = cancellationToken };

                await _client.SendToUserAsync(
                    userId: userStr,
                    content: requestContent,
                    contentType: ContentType.ApplicationJson,
                    context: requestContext)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WebPubSubService] SendToUser FAILED. Hub={Hub} UserId={UserId} Event={Event}",
                    _hubName, userStr, eventName);
            }
        }

        public async Task SendToGroupAsync(
            Guid sessionId,
            string eventName,
            object payload,
            CancellationToken cancellationToken = default)
        {
            var groupName = $"{ChatGroupPrefix}{sessionId}";
            _logger.LogInformation(
                "[WebPubSubService] SendToGroup started. Hub={Hub} Group={Group} Event={Event}",
                _hubName, groupName, eventName);

            try
            {
                var wrapped = new { type = eventName, data = payload };
                var json = JsonSerializer.Serialize(wrapped, _jsonOptions);
                var requestContent = RequestContent.Create(BinaryData.FromString(json));
                var requestContext = new global::Azure.RequestContext { CancellationToken = cancellationToken };

                await _client.SendToGroupAsync(
                    group: groupName,
                    content: requestContent,
                    contentType: ContentType.ApplicationJson,
                    excluded: null,
                    context: requestContext)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WebPubSubService] SendToGroup FAILED. Hub={Hub} Group={Group} Event={Event}",
                    _hubName, groupName, eventName);
            }
        }

        public async Task<Uri> GenerateClientAccessUriAsync(
            Guid userId,
            IEnumerable<string> groups,
            TimeSpan? tokenLifetime = null,
            CancellationToken cancellationToken = default)
        {
            var expiry = DateTimeOffset.UtcNow.Add(tokenLifetime ?? TimeSpan.FromHours(1));
            var userStr = userId.ToString();
            var roles = groups.Select(g => $"webpubsub.joinLeaveGroup.{g}").ToArray();

            _logger.LogInformation(
                "[WebPubSubService] Generating client access URI. Hub={Hub} UserId={UserId} Groups={Groups}",
                _hubName, userStr, string.Join(",", groups));

            var uri = _client.GetClientAccessUri(
                expiresAt: expiry,
                userId: userStr,
                roles: roles);

            return await Task.FromResult(uri);
        }

        private sealed record SessionStatusChangedPayload(
            string Type,
            Guid SessionId,
            string Title,
            string Status
        );
    }
}

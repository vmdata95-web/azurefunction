using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Hubs
{
    /// <summary>
    /// The SignalR hub for WebRTC live stream room signaling.
    /// Handles joining/leaving room, routing offers, answers, and ICE candidates between broadcaster and viewers.
    /// </summary>
    [Authorize]
    public class StreamHub : Hub
    {
        // Thread-safe collection mapping: sessionId -> list of broadcaster connection IDs
        private static readonly ConcurrentDictionary<string, ConcurrentHashSet<string>> _roomBroadcasters = new();
        // Thread-safe collection mapping: connectionId -> sessionId
        private static readonly ConcurrentDictionary<string, string> _connectionRooms = new();

        /// <summary>
        /// Join a stream room as either a broadcaster or a viewer.
        /// </summary>
        public async Task JoinStreamRoom(string sessionId, string role)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return;

            // Track room association for disconnection cleanup
            _connectionRooms[Context.ConnectionId] = sessionId;

            // Add the caller's connection to the SignalR group for the session
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

            if (role == "broadcaster")
            {
                var broadcasters = _roomBroadcasters.GetOrAdd(sessionId, _ => new ConcurrentHashSet<string>());
                broadcasters.Add(Context.ConnectionId);
            }
            else if (role == "viewer")
            {
                // Notify the broadcaster(s) in this room that a viewer has joined
                if (_roomBroadcasters.TryGetValue(sessionId, out var broadcasters))
                {
                    foreach (var broadcasterId in broadcasters.Keys)
                    {
                        await Clients.Client(broadcasterId).SendAsync("ViewerJoined", Context.ConnectionId);
                    }
                }
            }
        }

        /// <summary>
        /// Forward offer from broadcaster to a specific viewer.
        /// </summary>
        public async Task SendOffer(string viewerId, string offerJson)
        {
            await Clients.Client(viewerId).SendAsync("ReceiveOffer", Context.ConnectionId, offerJson);
        }

        /// <summary>
        /// Forward answer from viewer to broadcaster.
        /// </summary>
        public async Task SendAnswer(string broadcasterId, string answerJson)
        {
            await Clients.Client(broadcasterId).SendAsync("ReceiveAnswer", Context.ConnectionId, answerJson);
        }

        /// <summary>
        /// Forward ICE candidate between peers.
        /// </summary>
        public async Task SendIceCandidate(string targetId, string candidateJson)
        {
            await Clients.Client(targetId).SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidateJson);
        }

        /// <summary>
        /// Handles cleanup and notifications when a connection is closed.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionRooms.TryRemove(Context.ConnectionId, out var sessionId))
            {
                // If the disconnecting client was a broadcaster, remove them
                if (_roomBroadcasters.TryGetValue(sessionId, out var broadcasters))
                {
                    broadcasters.Remove(Context.ConnectionId);
                    if (broadcasters.IsEmpty)
                    {
                        _roomBroadcasters.TryRemove(sessionId, out _);
                    }
                }

                // Notify other clients in the group that this viewer/broadcaster left
                await Clients.OthersInGroup(sessionId).SendAsync("ViewerLeft", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// Thread-safe HashSet wrapper based on ConcurrentDictionary.
    /// </summary>
    public class ConcurrentHashSet<T> : ConcurrentDictionary<T, byte> where T : notnull
    {
        public bool Add(T item) => TryAdd(item, 0);
        public bool Remove(T item) => TryRemove(item, out _);
        public bool Contains(T item) => ContainsKey(item);
    }
}

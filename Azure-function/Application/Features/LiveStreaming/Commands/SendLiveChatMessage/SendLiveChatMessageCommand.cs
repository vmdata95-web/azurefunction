using Domain.Dto;
using MediatR;

namespace Application.Features.LiveStreaming.Commands.SendLiveChatMessage
{
    /// <summary>
    /// Sends a chat message in the context of an active live stream session.
    /// The message is persisted and broadcast to connected SignalR viewers.
    /// </summary>
    public class SendLiveChatMessageCommand : IRequest<SendLiveChatMessageResponseDto>
    {
        /// <summary>The session (live stream) the message is posted in.</summary>
        public Guid   SessionId   { get; set; }

        /// <summary>The user sending the message.</summary>
        public Guid   UserId      { get; set; }

        /// <summary>The message text to send.</summary>
        public string MessageText { get; set; } = string.Empty;
    }
}

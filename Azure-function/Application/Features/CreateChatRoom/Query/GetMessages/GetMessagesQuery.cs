using Domain.Dto;
using MediatR;

namespace Application.Features.CreateChatRoom.Query.GetMessages
{
    public class GetMessagesQuery : IRequest<List<GetMessagesResponse>>
    {
        public Guid ChatRoomId { get; set; }

        /// <summary>
        /// The UserId extracted from the JWT by the controller.
        /// Null when the caller is unauthenticated (anonymous read, if permitted).
        /// </summary>
        public Guid? CurrentUserId { get; set; }

        /// <summary>
        /// Set to true by the controller when the JWT principal is a speaker
        /// for the event that owns this chat room.
        /// Grants visibility of ALL private messages in the room.
        /// </summary>
        public bool IsSpeaker { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 50;
    }
}
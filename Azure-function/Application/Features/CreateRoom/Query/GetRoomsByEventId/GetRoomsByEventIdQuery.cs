using Domain.Dto;
using MediatR;

namespace Application.Features.CreateRoom.Query.GetRoomsByEventId
{
    public class GetRoomsByEventIdQuery : IRequest<List<RoomDto>>
    {
        public Guid EventId { get; set; }
    }
}

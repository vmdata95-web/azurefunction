using Domain.Dto;
using Domain.Interfaces;
using MediatR;

namespace Application.Features.CreateRoom.Query.GetRoomsByEventId
{
    public class GetRoomsByEventIdHandler : IRequestHandler<GetRoomsByEventIdQuery, List<RoomDto>>
    {
        private readonly IRoomRepository _repository;

        public GetRoomsByEventIdHandler(IRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoomDto>> Handle(GetRoomsByEventIdQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetRoomsByEventIdAsync(request.EventId);
        }
    }
}

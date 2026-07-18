using Application.Common.Exceptions;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateSession.Query.GetSessionsByRoom
{
    public class GetSessionsByRoomHandler : IRequestHandler<GetSessionsByRoomQuery, List<SessionDto>>
    {
        private readonly ISessionRepository _repository;

        public GetSessionsByRoomHandler(ISessionRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<SessionDto>> Handle(GetSessionsByRoomQuery request, CancellationToken cancellationToken)
        {
            // 🔥 enum to string
            //var roomName = ((RoomTypeEnum)request.RoomType).ToString();

            // 🔥 get room id
            var roomId = await _repository.GetRoomIdByNameAsync(request.RoomType);

            if (roomId == null)
                throw new BadRequestException("Room not found");

            // 🔥 get sessions
            return await _repository.GetSessionsByRoomIdAsync(roomId.Value);
        }
    }
}

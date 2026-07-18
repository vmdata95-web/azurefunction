using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateChatRoom.Query.GetChatRoom
{
    public class GetChatRoomsHandler
        : IRequestHandler<GetChatRoomsQuery, List<ChatRoom>>
    {
        private readonly IChatRoomRepository _repository;

        public GetChatRoomsHandler(IChatRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ChatRoom>> Handle(
            GetChatRoomsQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetAllAsync();
        }
    }
}

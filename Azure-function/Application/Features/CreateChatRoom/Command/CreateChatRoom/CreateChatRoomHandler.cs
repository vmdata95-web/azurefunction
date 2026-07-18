using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateChatRoom.Command.CreateChatRoom
{
    public class CreateChatRoomHandler
    : IRequestHandler<CreateChatRoomCommand, CreateChatRoomResponse>
    {
        private readonly IChatRoomRepository _repository;

        public CreateChatRoomHandler(IChatRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<CreateChatRoomResponse> Handle(
            CreateChatRoomCommand request,
            CancellationToken cancellationToken)
        {
            var chatRoom = new ChatRoom
            {
                EventId = request.EventId,
                Type = request.Type
            };

            var id = await _repository.CreateAsync(chatRoom);

            return new CreateChatRoomResponse
            {
                Id = id,
                Message = "Chat room created successfully"
            };
        }
    }
}

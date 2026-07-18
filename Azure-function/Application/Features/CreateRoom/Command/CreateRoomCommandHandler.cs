using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateRoom.Command
{
    public class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, Guid>
    {
        private readonly IRoomRepository _repository;

        public CreateRoomCommandHandler(IRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
        {
            // ✅ Step 1: Check Event Exists
            var eventExists = await _repository.EventExistsAsync(request.EventId);

            if (!eventExists)
                throw new Exception("Event not found");

            // ✅ Step 2: Create Room Entity
            var room = new Room
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                Name = request.Name,
                Type = request.Type,
                LayoutJson = request.LayoutJson,
                CreatedAt = DateTime.UtcNow
            };

            // ✅ Step 3: Save
            return await _repository.CreateRoomAsync(room);
        }
    }
}

using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateChatRoom.Command.CreateChatRoom
{
    public class CreateChatRoomCommand : IRequest<CreateChatRoomResponse>
    {
        public Guid EventId { get; set; }

        public string Type { get; set; } = string.Empty;
    }
}

using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateChatRoom.Command.SendMessage
{
    public class SendMessageCommand : IRequest<Guid>
    {
        public Guid ChatRoomId { get; set; }

        public Guid UserId { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}

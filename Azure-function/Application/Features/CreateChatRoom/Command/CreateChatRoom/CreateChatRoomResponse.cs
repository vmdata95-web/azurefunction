using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateChatRoom.Command.CreateChatRoom
{
    public class CreateChatRoomResponse
    {
        public Guid Id { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}

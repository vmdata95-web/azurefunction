using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateChatRoom.Command.CreateSpeakerReply
{
    public class CreateSpeakerReplyCommand : IRequest<Guid>
    {
        public Guid ChatRoomId { get; set; }

        public Guid UserId { get; set; }   // speaker id

        public string Message { get; set; } = string.Empty;

        // public / private
        public string MessageType { get; set; } = "public";

        // private ke liye required
        public Guid? ReceiverUserId { get; set; }

        // optional reply chain
        public Guid? ReplyToMessageId { get; set; }
    }
}

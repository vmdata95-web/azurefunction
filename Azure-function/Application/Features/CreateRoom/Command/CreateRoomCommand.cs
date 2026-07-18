using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateRoom.Command
{
    public class CreateRoomCommand : IRequest<Guid>
    {
        public Guid EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string LayoutJson { get; set; } = "{}";
    }
}

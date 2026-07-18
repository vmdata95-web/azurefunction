using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
namespace Application.Features.CreateSpeaker.Command.CreateSpeaker
{
    public class CreateSpeakerCommand : IRequest<Guid>
    {
        public Guid UserId { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
    }
}

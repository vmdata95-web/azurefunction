using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateExhibitor.Command.Lobby_video
{
    public class CreateExhibitorCommand : IRequest<Guid>
    {
        public Guid EventId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
             
        public string? Website { get; set; }

        public IFormFile? Logo { get; set; }
    }
}

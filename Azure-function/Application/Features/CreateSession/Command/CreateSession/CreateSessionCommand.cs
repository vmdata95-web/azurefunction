
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateSession.Command.CreateSession
{
    public class CreateSessionCommand : IRequest<Guid>
    {
        /// <summary>
        /// The event this session belongs to.
        /// Used as a path segment in Azure Blob Storage:
        /// <c>session-videos/{EventId}/{SpeakerId}/...</c>
        /// </summary>
        //public Guid EventId { get; set; }

        public Guid RoomId { get; set; }
        public Guid SpeakerId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public IFormFile? Video { get; set; }
        public string Status { get; set; } = "upcoming";
    }
}

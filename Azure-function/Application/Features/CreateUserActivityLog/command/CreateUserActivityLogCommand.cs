using Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateUserActivityLog.command
{
    public class CreateUserActivityLogCommand : IRequest<Guid>
    {
        public Guid UserId { get; set; }

        public Guid EventId { get; set; }

        public UserActivityAction Action { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public string Metadata { get; set; } = "{}";
    }
}

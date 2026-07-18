using Domain.Dto;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Query.GetEventAttendance
{
    public class GetEventAttendanceQuery : IRequest<EventAttendanceDto>
    {
        public Guid EventId { get; set; }

        public GetEventAttendanceQuery(Guid eventId)
        {
            EventId = eventId;
        }
    }
}

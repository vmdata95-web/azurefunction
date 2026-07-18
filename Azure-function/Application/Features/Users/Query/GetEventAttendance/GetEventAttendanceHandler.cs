using Domain.Dto;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Query.GetEventAttendance
{
    public class GetEventAttendanceHandler
         : IRequestHandler<GetEventAttendanceQuery, EventAttendanceDto>
    {
        private readonly IUserActivityLogRepository _repo;

        public GetEventAttendanceHandler(
            IUserActivityLogRepository repo)
        {
            _repo = repo;
        }

        public async Task<EventAttendanceDto> Handle(
            GetEventAttendanceQuery request,
            CancellationToken cancellationToken)
        {
            var totalRegistered =
                await _repo.GetTotalRegisteredAsync(request.EventId);

            var totalAttended =
                await _repo.GetTotalAttendedAsync(request.EventId);

            var totalNotAttended =
                await _repo.GetTotalNotAttendedAsync(request.EventId);

            return new EventAttendanceDto
            {
                TotalRegistered = totalRegistered,
                TotalAttended = totalAttended,
                TotalNotAttended = totalNotAttended
            };
        }
    }
}

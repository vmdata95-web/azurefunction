using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Query.GetLiveUsers
{
    public class GetLiveUsersHandler
        : IRequestHandler<GetLiveUsersQuery, int>
    {
        private readonly IUserActivityLogRepository _repo;

        public GetLiveUsersHandler(
            IUserActivityLogRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> Handle(
            GetLiveUsersQuery request,
            CancellationToken cancellationToken)
        {
            return await _repo.GetLiveUsersAsync(request.EventId);
        }
    }
}

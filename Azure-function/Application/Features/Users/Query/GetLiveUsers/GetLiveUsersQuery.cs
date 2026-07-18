using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Query.GetLiveUsers
{
    public class GetLiveUsersQuery : IRequest<int>
    {
        public Guid EventId { get; set; }

        public GetLiveUsersQuery(Guid eventId)
        {
            EventId = eventId;
        }
    }
}

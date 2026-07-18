using Domain.Dto;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateUserActivityLog.Query
{
    public class GetUserRoomTimeQuery : IRequest<List<UserRoomTimeResponse>>
    {
    }
}

using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateSession.Query.GetSessionsByRoom
{
    public class GetSessionsByRoomQuery : IRequest<List<SessionDto>>
    {
        public string RoomType { get; set; }   // 1,2,3,4
    }
}

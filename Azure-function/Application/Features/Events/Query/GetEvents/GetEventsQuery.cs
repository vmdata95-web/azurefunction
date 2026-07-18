using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Dto;
using MediatR;

namespace Application.Features.Events.Query.GetEvents
{
    public class GetEventsQuery : IRequest<List<EventDto>>
    {
        public string Type { get; set; }

        public GetEventsQuery(string type)
        {
            Type = type;
        }
    }
}
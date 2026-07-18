using Domain.Dto;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Events.Query.GetEvents
{
    public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, List<EventDto>>
    {
        private readonly IEventRepository _repository;

        public GetEventsQueryHandler(IEventRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<EventDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetEventsAsync(request.Type);
        }
    }
}
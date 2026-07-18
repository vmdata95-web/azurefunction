using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.GetAgenda.Query.GetAgenda
{
    public class GetAgendaQueryHandler : IRequestHandler<GetAgendaQuery, List<AgendaResponse>>
    {
        private readonly IAgendaRepository _repository;

        public GetAgendaQueryHandler(IAgendaRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<AgendaResponse>> Handle(GetAgendaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetAgendaAsync(request.EventId);
        }
    }
}

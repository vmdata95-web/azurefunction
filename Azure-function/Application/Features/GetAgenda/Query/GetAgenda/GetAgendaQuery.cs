using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.GetAgenda.Query.GetAgenda
{
    public class GetAgendaQuery : IRequest<List<AgendaResponse>>
    {
        public Guid EventId { get; set; }
    }
}

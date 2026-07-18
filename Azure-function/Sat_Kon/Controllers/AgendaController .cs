using Application.Features.GetAgenda.Query.GetAgenda;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sat_Kon.Controllers
{
    [ApiController]
    [Route("api/agenda")]
    public class AgendaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AgendaController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("agenda_get")]
        [Authorize(Roles = "Speake")]
        public async Task<IActionResult> GetAgenda([FromQuery] Guid eventId)
        {
            var result = await _mediator.Send(new GetAgendaQuery
            {
                EventId = eventId
            });

            return Ok(result);
        }
    }
}

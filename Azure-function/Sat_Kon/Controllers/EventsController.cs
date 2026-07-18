using Application.Features.Events.Commands.CreateEvent;
using Application.Features.Events.Query.GetEvents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sat_Kon.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        [Authorize(Roles = "SuperAdmi")]
        public async Task<IActionResult> Create([FromForm] CreateEventCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new { Id = result });
        }


        [HttpGet("/Get_event")]
        //[Authorize(Roles = "SuperAdmi")]
        public async Task<IActionResult> GetEvents([FromQuery] string type = "all")
        {
            var result = await _mediator.Send(new GetEventsQuery(type));

            return Ok(new
            {
                success = true,
                count = result.Count,
                data = result
            });
        }

    }
}

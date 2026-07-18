using Application.Features.CreateRoom.Command;
using Application.Features.CreateRoom.Query.GetRoomsByEventId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sat_Kon.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RoomsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("rooms_create")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomCommand command)
        {
            var roomId = await _mediator.Send(command);

            return Ok(new
            {
                message = "Room created successfully",
                room_id = roomId
            });
        }

        [HttpGet("{eventId}")]
        [Authorize(Roles = "SuperAdmin,User")]
        public async Task<IActionResult> GetRoomsByEventId(Guid eventId)
        {
            var rooms = await _mediator.Send(new GetRoomsByEventIdQuery { EventId = eventId });
            return Ok(rooms);
        }

    }
}

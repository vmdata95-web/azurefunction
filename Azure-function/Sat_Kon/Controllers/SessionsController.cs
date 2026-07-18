using Application.Features.CreateSession.Command.CreateSession;
using Application.Features.CreateSession.Query.GetSessionsByRoom;
using Application.Features.CreateSession.Query.StreamVideo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Sat_Kon.Controllers
{


    [ApiController]
    [Route("api/sessions")]
    public class SessionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SessionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("sessions")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateSession([FromForm] CreateSessionCommand command)
        {
            var sessionId = await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = "Session created successfully",
                data = new
                {
                    sessionId
                }
            });
        }

        [HttpGet("by-room")]
        [Authorize(Roles = "User,SuperAdmin")]
        public async Task<IActionResult> GetByRoom([FromQuery] string roomType)
        {
            var result = await _mediator.Send(new GetSessionsByRoomQuery
            {
                RoomType = roomType
            });

            return Ok(new
            {
                success = true,
                count = result.Count,
                data = result
            });
        }


        //[HttpGet("stream")]
        //[Authorize(Roles = "User")]
        //public async Task<IActionResult> Stream([FromQuery] string fileName)
        //{
        //    var result = await _mediator.Send(new StreamVideoQuery
        //    {
        //        FileName = fileName
        //    });

        //    return File(
        //        result.stream,
        //        result.contentType,
        //        enableRangeProcessing: true
        //    );
        //}
    }
}

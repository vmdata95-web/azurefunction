using Application.Features.CreateUserActivityLog.command;
using Application.Features.CreateUserActivityLog.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sat_Kon.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/user-activity-logs")]
    public class UserActivityLogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserActivityLogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("user_activity")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Create(
            [FromBody] CreateUserActivityLogCommand command
        )
        {
            var id = await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = "User activity log created successfully",
                data = id
            });
        }

        [HttpGet("room-time")]
        //[Authorize(Roles = "SuperAdmi")]
        public async Task<IActionResult> GetRoomTime()
        {
            var result = await _mediator.Send(
                new GetUserRoomTimeQuery()
            );

            return Ok(new
            {
                success = true,
                message = "User room time fetched successfully",
                data = result
            });
        }
    }
}

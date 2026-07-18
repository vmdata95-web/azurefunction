using Application.Features.CreateExhibitor.Command.Lobby_video;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Sat_Kon.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExhibitorController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ExhibitorController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // ════════════════════════════════════════════════════════════════════
        // POST /api/Exhibitor/Lobby_video
        // ════════════════════════════════════════════════════════════════════

        [HttpPost("Lobby_video")]
        [Authorize(Roles = "SuperAdmi")]
        public async Task<IActionResult> Create(
            [FromForm] CreateExhibitorCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}

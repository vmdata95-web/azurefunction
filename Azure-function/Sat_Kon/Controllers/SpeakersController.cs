using Application.Features.CreateSpeaker.Command.CreateSpeaker;
using Application.Features.CreateSpeaker.Query.GetSpeakers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/speakers")]
public class SpeakersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpeakersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("speakers_create")]
    [Authorize(Roles = "SuperAdmi")]
    public async Task<IActionResult> CreateSpeaker([FromBody] CreateSpeakerCommand command)
    {
        var speakerId = await _mediator.Send(command);

        return Ok(new
        {
            message = "Speaker created successfully",
            speaker_id = speakerId
        });
    }

    [HttpGet("speakers_get")]
    [Authorize(Roles = "SuperAdmin,Speaker")]
    public async Task<IActionResult> GetSpeakers()
    {
        var speakers = await _mediator.Send(new GetSpeakersQuery());
        return Ok(speakers);
    }
}
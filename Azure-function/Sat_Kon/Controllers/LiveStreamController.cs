using Application.Features.LiveStreaming.Commands.SendLiveChatMessage;
using Application.Features.LiveStreaming.Query.GetLiveSession;
using Domain.Dto;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sat_Kon.Controllers
{
    /// <summary>
    /// Handles all live-stream REST endpoints.
    /// Routes are prefixed with /api/live-stream.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/live-stream")]
    public class LiveStreamController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LiveStreamController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // ────────────────────────────────────────────────────────────────
        // POST /api/live-stream/start
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts a live stream for an existing session.
        /// </summary>
        /// <remarks>
        /// Business rules enforced by the command handler:
        ///   • Session must exist.
        ///   • Room must exist.
        ///   • Speaker must exist.
        ///   • Session must not already be live (no duplicate active streams).
        /// </remarks>
        //[HttpPost("start")]
        //[ProducesResponseType(typeof(StartLiveStreamResponseDto), StatusCodes.Status201Created)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status409Conflict)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> StartLiveStream(
        //    [FromBody] StartLiveStreamRequestDto request,
        //    CancellationToken cancellationToken)
        //{
        //    var command = new StartLiveStreamCommand
        //    {
        //        SessionId = request.SessionId,
        //        RoomId    = request.RoomId,
        //        SpeakerId = request.SpeakerId
        //    };

        //    var result = await _mediator.Send(command, cancellationToken);

        //    return StatusCode(StatusCodes.Status201Created, result);
        //}

        // ────────────────────────────────────────────────────────────────
        // POST /api/live-stream/stop
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Stops an active live stream for the given session.
        /// </summary>
        /// <remarks>
        /// Business rules enforced by the command handler:
        ///   • Session must exist.
        ///   • Session must currently have an active (Live) live stream.
        ///   • Status is transitioned to Ended and EndedAt is stamped.
        ///   • Session.Status is also updated to "ended".
        /// </remarks>
        //[HttpPost("stop")]
        //[ProducesResponseType(typeof(StopLiveStreamResponseDto), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status409Conflict)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> StopLiveStream(
        //    [FromBody] StopLiveStreamRequestDto request,
        //    CancellationToken cancellationToken)
        //{
        //    var command = new StopLiveStreamCommand
        //    {
        //        SessionId = request.SessionId,
        //        SpeakerId = request.SpeakerId
        //    };

        //    var result = await _mediator.Send(command, cancellationToken);

        //    return Ok(result);
        //}

        // ────────────────────────────────────────────────────────────────
        // POST /api/live-stream/join
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Allows a user to join an active live stream.
        /// </summary>
        /// <remarks>
        /// Business rules enforced by the command handler:
        ///   • Session must exist.
        ///   • Session must currently be live.
        ///   • User must exist.
        ///   • Join activity is recorded in UserActivityLogs (Action = Enter).
        ///   • Stream key and session details are returned for the viewer.
        /// </remarks>
        //[HttpPost("join")]
        //[ProducesResponseType(typeof(JoinLiveStreamResponseDto), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status409Conflict)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> JoinLiveStream(
        //    [FromBody] JoinLiveStreamRequestDto request,
        //    CancellationToken cancellationToken)
        //{
        //    var command = new JoinLiveStreamCommand
        //    {
        //        SessionId = request.SessionId,
        //        UserId    = request.UserId
        //    };

        //    var result = await _mediator.Send(command, cancellationToken);

        //    return Ok(result);
        //}

        // ────────────────────────────────────────────────────────────────
        // POST /api/live-stream/leave
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Allows a user to leave a live stream session.
        /// </summary>
        /// <remarks>
        /// Business rules enforced by the command handler:
        ///   • Session must exist.
        ///   • User must exist.
        ///   • Leave activity is recorded in UserActivityLogs (Action = Out).
        ///   • Allowed even after the stream has ended (graceful disconnect).
        /// </remarks>
        //[HttpPost("leave")]
        //[ProducesResponseType(typeof(LeaveLiveStreamResponseDto), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> LeaveLiveStream(
        //    [FromBody] LeaveLiveStreamRequestDto request,
        //    CancellationToken cancellationToken)
        //{
        //    var command = new LeaveLiveStreamCommand
        //    {
        //        SessionId = request.SessionId,
        //        UserId    = request.UserId
        //    };

        //    var result = await _mediator.Send(command, cancellationToken);

        //    return Ok(result);
        //}

        // ────────────────────────────────────────────────────────────────
        // POST /api/live-stream/chat/send
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a chat message inside an active live stream room.
        /// </summary>
        /// <remarks>
        /// Business rules enforced by the command handler:
        ///   • Session must exist and must currently be live.
        ///   • User must exist.
        ///   • Message text must not be empty (max 2000 chars).
        ///   • Message is persisted to the database using the existing Message entity.
        ///   • Message is broadcast to SignalR viewers via IChatHubService.
        /// </remarks>
        //[HttpPost("chat/send")]
        //[ProducesResponseType(typeof(SendLiveChatMessageResponseDto), StatusCodes.Status201Created)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status409Conflict)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> SendLiveChatMessage(
        //    [FromBody] SendLiveChatMessageRequestDto request,
        //    CancellationToken cancellationToken)
        //{
        //    var command = new SendLiveChatMessageCommand
        //    {
        //        SessionId   = request.SessionId,
        //        UserId      = request.UserId,
        //        MessageText = request.MessageText
        //    };

        //    var result = await _mediator.Send(command, cancellationToken);

        //    return StatusCode(StatusCodes.Status201Created, result);
        //}

        // ────────────────────────────────────────────────────────────────
        // GET /api/live-stream/session/{sessionId}
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Gets live session details.
        /// </summary>
        //[HttpGet("session/{sessionId}")]
        //[ProducesResponseType(typeof(GetLiveSessionResponseDto), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> GetLiveSession(
        //    [FromRoute] Guid sessionId,
        //    CancellationToken cancellationToken)
        //{
        //    var query = new GetLiveSessionQuery { SessionId = sessionId };
        //    var result = await _mediator.Send(query, cancellationToken);

        //    if (result == null)
        //    {
        //        return NotFound(new { message = $"No live session found for id '{sessionId}'." });
        //    }

        //    return Ok(result);
        //}

        // ────────────────────────────────────────────────────────────────
        // GET /api/live-stream/active
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Gets all active live streams.
        /// </summary>
        //[HttpGet("active")]
        //[ProducesResponseType(typeof(IEnumerable<ActiveStreamResponseDto>), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> GetActiveStreams(CancellationToken cancellationToken)
        //{
        //    var query = new GetActiveStreamsQuery();
        //    var result = await _mediator.Send(query, cancellationToken);

        //    return Ok(result);
        //}
    }
}

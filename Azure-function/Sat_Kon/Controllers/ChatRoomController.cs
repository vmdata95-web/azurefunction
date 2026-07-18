using Application.Features.CreateChatRoom.Command.CreateChatRoom;
using Application.Features.CreateChatRoom.Command.CreateSpeakerReply;
using Application.Features.CreateChatRoom.Command.SendMessage;
using Application.Features.CreateChatRoom.Query.GetChatRoom;
using Application.Features.CreateChatRoom.Query.GetMessages;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Sat_Kon.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/chat-room")]
    public class ChatRoomController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ChatRoomController> _logger;

        public ChatRoomController(IMediator mediator, ILogger<ChatRoomController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost("Createchatroom")]
        [Authorize(Roles = "SuperAdmi")]
        public async Task<IActionResult> Create(
            [FromBody] CreateChatRoomCommand command)
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        [HttpGet("Getchatroom")]
        [Authorize(Roles = "User,Speaker")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetChatRoomsQuery());

            return Ok(result);
        }

        [HttpPost("{chatRoomId}/messages")]
        [Authorize(Roles = "SuperAdmi")]
        public async Task<IActionResult> SendMessage(
            Guid chatRoomId,
            [FromBody] SendMessageCommand command)
        {
            var correlationId = HttpContext.TraceIdentifier;
            
            _logger.LogInformation("[ChatRoomController] SendMessage endpoint invoked. Correlation ID: {CorrelationId}, ChatRoomId: {ChatRoomId}", 
                correlationId, chatRoomId);

            try
            {
                command.ChatRoomId = chatRoomId;

                var result = await _mediator.Send(command);

                _logger.LogInformation("[ChatRoomController] SendMessage successfully processed. Correlation ID: {CorrelationId}, MessageId: {MessageId}", 
                    correlationId, result);

                return Ok(new
                {
                    MessageId = result,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ChatRoomController] SendMessage threw an exception. Correlation ID: {CorrelationId}, ChatRoomId: {ChatRoomId}", 
                    correlationId, chatRoomId);

                // Temporarily return full exception details to client for debugging in production
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while sending the message.",
                    CorrelationId = correlationId,
                    DebugDetails = new
                    {
                        ExceptionType = ex.GetType().FullName,
                        ExceptionMessage = ex.Message,
                        InnerExceptionMessage = ex.InnerException?.Message,
                        StackTrace = ex.StackTrace
                    }
                });
            }
        }

        
        [HttpGet("{chatRoomId}/messages")]
        [Authorize(Roles = "User,Speaker")]
        public async Task<IActionResult> GetMessages(
            Guid chatRoomId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            // Extract caller identity from JWT
            Guid? currentUserId = null;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out Guid parsedId))
            {
                currentUserId = parsedId;
            }

            // Extract role — the handler will verify this against the DB
            // to prevent a tampered JWT from granting speaker-level visibility.
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var isSpeakerClaim = string.Equals(roleClaim, "speaker", StringComparison.OrdinalIgnoreCase);

            var result = await _mediator.Send(
                new GetMessagesQuery
                {
                    ChatRoomId    = chatRoomId,
                    CurrentUserId = currentUserId,
                    IsSpeaker     = isSpeakerClaim,
                    Page          = page,
                    PageSize      = pageSize
                });

            return Ok(result);
        }

        [HttpPost("reply")]
        [Authorize(Roles = "SuperAdmi")]
        public async Task<IActionResult> Reply([FromBody] CreateSpeakerReplyCommand command, CancellationToken cancellationToken)
        {
            var messageId = await _mediator.Send(command, cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Message sent successfully",
                id = messageId
            });
        }
    }
}

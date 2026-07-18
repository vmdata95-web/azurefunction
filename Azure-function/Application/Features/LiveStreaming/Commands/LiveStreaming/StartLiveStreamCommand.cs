//using Domain.Dto;
//using MediatR;

//namespace Application.Features.LiveStreaming.Commands.LiveStreaming
//{
//    /// <summary>
//    /// MediatR command that carries everything the handler needs
//    /// to validate and start a live stream for a session.
//    /// </summary>
//    public class StartLiveStreamCommand : IRequest<StartLiveStreamResponseDto>
//    {
//        /// <summary>The session that should go live.</summary>
//        public Guid SessionId { get; set; }

//        /// <summary>The room in which the session is held.</summary>
//        public Guid RoomId { get; set; }

//        /// <summary>The speaker who is starting the stream.</summary>
//        public Guid SpeakerId { get; set; }
//    }
//}

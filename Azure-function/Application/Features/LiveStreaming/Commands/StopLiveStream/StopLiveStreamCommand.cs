//using Domain.Dto;
//using MediatR;

//namespace Application.Features.LiveStreaming.Commands.StopLiveStream
//{
//    /// <summary>
//    /// Stops an active live stream for a given session.
//    /// </summary>
//    public class StopLiveStreamCommand : IRequest<StopLiveStreamResponseDto>
//    {
//        /// <summary>The session whose live stream should be stopped.</summary>
//        public Guid SessionId { get; set; }

//        /// <summary>The speaker stopping the stream (must be the stream owner).</summary>
//        public Guid SpeakerId { get; set; }
//    }
//}

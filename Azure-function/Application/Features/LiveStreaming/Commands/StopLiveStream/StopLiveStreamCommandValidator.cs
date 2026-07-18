//using FluentValidation;

//namespace Application.Features.LiveStreaming.Commands.StopLiveStream
//{
//    /// <summary>
//    /// FluentValidation validator for <see cref="StopLiveStreamCommand"/>.
//    /// Auto-discovered by AddValidatorsFromAssemblyContaining&lt;AssemblyMarker&gt;.
//    /// </summary>
//    public class StopLiveStreamCommandValidator : AbstractValidator<StopLiveStreamCommand>
//    {
//        public StopLiveStreamCommandValidator()
//        {
//            RuleFor(x => x.SessionId)
//                .NotEmpty()
//                .WithMessage("SessionId is required.");

//            RuleFor(x => x.SpeakerId)
//                .NotEmpty()
//                .WithMessage("SpeakerId is required.");
//        }
//    }
//}

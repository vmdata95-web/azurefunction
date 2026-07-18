//using FluentValidation;

//namespace Application.Features.LiveStreaming.Commands.LiveStreaming
//{
//    /// <summary>
//    /// FluentValidation validator for <see cref="StartLiveStreamCommand"/>.
//    /// Registered automatically by AddValidatorsFromAssemblyContaining&lt;AssemblyMarker&gt;()
//    /// and invoked via the existing ValidationBehavior MediatR pipeline.
//    /// </summary>
//    public class StartLiveStreamCommandValidator : AbstractValidator<StartLiveStreamCommand>
//    {
//        public StartLiveStreamCommandValidator()
//        {
//            RuleFor(x => x.SessionId)
//                .NotEmpty()
//                .WithMessage("SessionId is required.");

//            RuleFor(x => x.RoomId)
//                .NotEmpty()
//                .WithMessage("RoomId is required.");

//            RuleFor(x => x.SpeakerId)
//                .NotEmpty()
//                .WithMessage("SpeakerId is required.");
//        }
//    }
//}

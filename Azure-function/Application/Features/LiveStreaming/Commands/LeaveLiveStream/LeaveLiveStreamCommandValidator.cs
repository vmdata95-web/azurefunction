//using FluentValidation;

//namespace Application.Features.LiveStreaming.Commands.LeaveLiveStream
//{
//    /// <summary>
//    /// FluentValidation validator for <see cref="LeaveLiveStreamCommand"/>.
//    /// </summary>
//    public class LeaveLiveStreamCommandValidator : AbstractValidator<LeaveLiveStreamCommand>
//    {
//        public LeaveLiveStreamCommandValidator()
//        {
//            RuleFor(x => x.SessionId)
//                .NotEmpty()
//                .WithMessage("SessionId is required.");

//            RuleFor(x => x.UserId)
//                .NotEmpty()
//                .WithMessage("UserId is required.");
//        }
//    }
//}

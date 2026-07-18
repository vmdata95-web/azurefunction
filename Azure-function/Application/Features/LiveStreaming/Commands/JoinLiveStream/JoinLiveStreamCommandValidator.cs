//using FluentValidation;

//namespace Application.Features.LiveStreaming.Commands.JoinLiveStream
//{
//    /// <summary>
//    /// FluentValidation validator for <see cref="JoinLiveStreamCommand"/>.
//    /// </summary>
//    public class JoinLiveStreamCommandValidator : AbstractValidator<JoinLiveStreamCommand>
//    {
//        public JoinLiveStreamCommandValidator()
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

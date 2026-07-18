using FluentValidation;

namespace Application.Features.LiveStreaming.Commands.SendLiveChatMessage
{
    /// <summary>
    /// FluentValidation validator for <see cref="SendLiveChatMessageCommand"/>.
    /// </summary>
    public class SendLiveChatMessageCommandValidator : AbstractValidator<SendLiveChatMessageCommand>
    {
        public SendLiveChatMessageCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty()
                .WithMessage("SessionId is required.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required.");

            RuleFor(x => x.MessageText)
                .NotEmpty()
                .WithMessage("Message text cannot be empty.")
                .MaximumLength(2000)
                .WithMessage("Message text cannot exceed 2000 characters.");
        }
    }
}

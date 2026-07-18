using FluentValidation;

namespace Application.Features.CreateChatRoom.Command.SendMessage
{
    public class SendMessageValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageValidator()
        {
            RuleFor(x => x.ChatRoomId)
                .NotEmpty()
                .WithMessage("ChatRoomId is required.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required.");

            RuleFor(x => x.Message)
                .NotEmpty()
                .WithMessage("Message text cannot be empty.")
                .MaximumLength(5000)
                .WithMessage("Message cannot exceed 5000 characters.");

            // MessageType is intentionally NOT accepted from the frontend.
            // The handler always forces MessageType = "private" for attendee questions.
        }
    }
}

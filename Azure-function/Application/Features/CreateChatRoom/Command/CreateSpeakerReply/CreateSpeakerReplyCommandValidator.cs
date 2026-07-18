using FluentValidation;

namespace Application.Features.CreateChatRoom.Command.CreateSpeakerReply
{
    public class CreateSpeakerReplyCommandValidator
        : AbstractValidator<CreateSpeakerReplyCommand>
    {
        public CreateSpeakerReplyCommandValidator()
        {
            RuleFor(x => x.ChatRoomId)
                .NotEmpty()
                .WithMessage("ChatRoomId is required.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId (speaker) is required.");

            RuleFor(x => x.ReplyToMessageId)
                .NotEmpty()
                .WithMessage("ReplyToMessageId is required. A speaker reply must reference an existing message.");

            RuleFor(x => x.Message)
                .NotEmpty()
                .WithMessage("Reply text cannot be empty.")
                .MaximumLength(4000)
                .WithMessage("Reply text cannot exceed 4000 characters.");

            RuleFor(x => x.MessageType)
                .NotEmpty()
                .Must(t => t == "public" || t == "private")
                .WithMessage("MessageType must be 'public' or 'private'.");

            // ReceiverUserId is intentionally NOT validated here.
            // The handler enforces it server-side:
            //   - private → ReceiverUserId = originalMessage.UserId  (auto-set)
            //   - public  → ReceiverUserId = null                    (auto-cleared)
            // Any client-supplied ReceiverUserId is silently ignored.
        }
    }
}

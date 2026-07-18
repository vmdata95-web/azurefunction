using FluentValidation;

namespace Application.Features.RoomImage.Command.UploadRoomImage
{
    /// <summary>
    /// FluentValidation validator for <see cref="UploadRoomImageCommand"/>.
    /// Runs automatically through the MediatR ValidationBehavior pipeline.
    /// </summary>
    public class UploadRoomImageValidator : AbstractValidator<UploadRoomImageCommand>
    {
        private static readonly HashSet<string> _allowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
            };

        // 10 MB maximum image size
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public UploadRoomImageValidator()
        {
            RuleFor(x => x.RoomId)
                .NotEmpty()
                .WithMessage("RoomId is required.");

            RuleFor(x => x.Image)
                .NotNull()
                .WithMessage("An image file is required.");

            When(x => x.Image is not null, () =>
            {
                RuleFor(x => x.Image!.Length)
                    .GreaterThan(0)
                    .WithMessage("The image file must not be empty.")
                    .LessThanOrEqualTo(MaxFileSizeBytes)
                    .WithMessage($"Image size must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.");

                RuleFor(x => System.IO.Path.GetExtension(x.Image!.FileName))
                    .Must(ext => _allowedExtensions.Contains(ext))
                    .WithMessage(
                        $"Unsupported file type. Allowed: {string.Join(", ", _allowedExtensions)}");
            });
        }
    }
}

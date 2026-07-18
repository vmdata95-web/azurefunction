using FluentValidation;

namespace Application.Features.RoomImage.Query.GetRoomImage
{
    /// <summary>
    /// Validates <see cref="GetRoomImageQuery"/>.
    /// At least one of ImageId, RoomId, or RoomName must be supplied.
    /// </summary>
    public class GetRoomImageValidator : AbstractValidator<GetRoomImageQuery>
    {
        public GetRoomImageValidator()
        {
            RuleFor(x => x)
                .Must(x => x.ImageId.HasValue
                         || x.RoomId.HasValue
                         || !string.IsNullOrWhiteSpace(x.RoomName))
                .WithMessage("At least one of ImageId, RoomId, or RoomName must be provided.");
        }
    }
}

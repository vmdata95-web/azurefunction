using FluentValidation;

namespace Application.Features.LiveStreaming.Query.GetLiveSession
{
    public class GetLiveSessionQueryValidator : AbstractValidator<GetLiveSessionQuery>
    {
        public GetLiveSessionQueryValidator()
        {
            RuleFor(v => v.SessionId)
                .NotEmpty().WithMessage("SessionId is required.");
        }
    }
}

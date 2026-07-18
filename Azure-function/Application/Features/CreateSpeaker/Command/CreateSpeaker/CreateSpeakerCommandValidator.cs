using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateSpeaker.Command.CreateSpeaker
{
    public class CreateSpeakerCommandValidator : AbstractValidator<CreateSpeakerCommand>
    {
        public CreateSpeakerCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            RuleFor(x => x.Bio)
                .NotEmpty().WithMessage("Bio is required");

            RuleFor(x => x.Company)
                .MaximumLength(150);

            RuleFor(x => x.Website)
                .Must(BeValidUrl)
                .When(x => !string.IsNullOrWhiteSpace(x.Website))
                .WithMessage("Invalid website URL");
        }

        private bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}

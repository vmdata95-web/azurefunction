using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateSession.Command.CreateSession
{
    public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
    {
        public CreateSessionCommandValidator()
        {
            RuleFor(x => x.RoomId).NotEmpty();
            RuleFor(x => x.SpeakerId).NotEmpty();

            RuleFor(x => x.Title)
                .NotEmpty();

            RuleFor(x => x.StartTime)
                .LessThan(x => x.EndTime)
                .WithMessage("StartTime must be less than EndTime");

            //RuleFor(x => x.Video)
            //    .NotNull().WithMessage("Video is required");
        }

        private bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}

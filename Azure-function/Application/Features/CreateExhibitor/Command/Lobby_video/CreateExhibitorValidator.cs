using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateExhibitor.Command.Lobby_video
{
    public class CreateExhibitorValidator
    : AbstractValidator<CreateExhibitorCommand>
    {
        public CreateExhibitorValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.EventId)
                .NotEmpty();

            RuleFor(x => x.Logo)
                .NotNull()
                .WithMessage("Video is required");
        }
    }
}

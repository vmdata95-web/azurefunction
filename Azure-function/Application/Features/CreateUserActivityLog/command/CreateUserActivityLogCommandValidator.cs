using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateUserActivityLog.command
{
    public class CreateUserActivityLogCommandValidator
     : AbstractValidator<CreateUserActivityLogCommand>
    {
        public CreateUserActivityLogCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.EventId)
                .NotEmpty();

            RuleFor(x => x.Action)
                .IsInEnum();

            RuleFor(x => x.RoomName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Metadata)
                .NotEmpty();
        }
    }
}

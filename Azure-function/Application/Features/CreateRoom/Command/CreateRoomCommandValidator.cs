using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateRoom.Command
{
    public class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
    {
        public CreateRoomCommandValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("EventId is required");

            RuleFor(x => x.Name)
                .NotEmpty().MaximumLength(100);

            RuleFor(x => x.Type)
                .NotEmpty().MaximumLength(50);

            RuleFor(x => x.LayoutJson)
                .NotEmpty().WithMessage("LayoutJson is required");
        }
    }
}

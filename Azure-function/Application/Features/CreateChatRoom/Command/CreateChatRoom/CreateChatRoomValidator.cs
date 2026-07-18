using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateChatRoom.Command.CreateChatRoom
{
    public class CreateChatRoomValidator : AbstractValidator<CreateChatRoomCommand>
    {
        public CreateChatRoomValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty()
                .WithMessage("EventId is required");

            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("Type is required");

            
        }
    }
}

//using FluentValidation;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Application.Features.CreateSession.Query.GetSessionsByRoom
//{
//    public class GetSessionsByRoomValidator : AbstractValidator<GetSessionsByRoomQuery>
//    {
//        public GetSessionsByRoomValidator()
//        {
//            RuleFor(x => x.RoomType)
//                .InclusiveBetween(1, 4)
//                .WithMessage("RoomType must be between 1 to 4");
//        }
//    }
//}

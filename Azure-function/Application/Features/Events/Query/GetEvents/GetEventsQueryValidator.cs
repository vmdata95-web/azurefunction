//using FluentValidation;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using FluentValidation;

//namespace Application.Features.Events.Query.GetEvents
//{
//    public class GetEventsQueryValidator : AbstractValidator<GetEventsQuery>
//    {
//        public GetEventsQueryValidator()
//        {
//            RuleFor(x => x.Status)
//                .MaximumLength(50)
//                .When(x => !string.IsNullOrEmpty(x.Status));  // ✅ optional
//        }
//    }
//}

//using Domain.Dto;
//using Domain.Interfaces;
//using MediatR;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Application.Features.LiveStreaming.Query.GetActiveStreams
//{
//    public class GetActiveStreamsQueryHandler : IRequestHandler<GetActiveStreamsQuery, IEnumerable<ActiveStreamResponseDto>>
//    {
//        private readonly ILiveStreamRepository _repository;

//        public GetActiveStreamsQueryHandler(ILiveStreamRepository repository)
//        {
//            _repository = repository;
//        }

//        public async Task<IEnumerable<ActiveStreamResponseDto>> Handle(GetActiveStreamsQuery request, CancellationToken cancellationToken)
//        {
//            var result = await _repository.GetActiveStreamsAsync(cancellationToken);
//            return result;
//        }
//    }
//}

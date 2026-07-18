using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateExhibitor.Query.GetExhibitorVideos
{
    public class GetExhibitorVideosHandler
        : IRequestHandler<GetExhibitorVideosQuery, List<string>>
    {
        private readonly IExhibitorRepository _repository;

        public GetExhibitorVideosHandler(
            IExhibitorRepository repository)
        {
            _repository = repository;
        }
        public async Task<List<string>> Handle(
    GetExhibitorVideosQuery request,
    CancellationToken cancellationToken)
        {
            var videos = await _repository.GetByEventIdAsync(request.EventId);

            return videos;
        }
    }
}

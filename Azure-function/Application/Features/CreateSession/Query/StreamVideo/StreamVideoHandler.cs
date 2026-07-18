using Domain.Interfaces;
using MediatR;

namespace Application.Features.CreateSession.Query.StreamVideo
{
    public class StreamVideoHandler
        : IRequestHandler<StreamVideoQuery, (Stream stream, string contentType)>
    {
        private readonly IVideoRepository _repository;

        public StreamVideoHandler(IVideoRepository repository)
        {
            _repository = repository;
        }

        public async Task<(Stream stream, string contentType)> Handle(
            StreamVideoQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetVideoStreamAsync(request.FileName);
        }
    }
}
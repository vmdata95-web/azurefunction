using Domain.Dto;
using Domain.Interfaces;
using MediatR;

namespace Application.Features.CreateSpeaker.Query.GetSpeakers
{
    public class GetSpeakersHandler : IRequestHandler<GetSpeakersQuery, List<SpeakerDto>>
    {
        private readonly ISpeakerRepository _repository;

        public GetSpeakersHandler(ISpeakerRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<SpeakerDto>> Handle(GetSpeakersQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetSpeakersAsync();
        }
    }
}

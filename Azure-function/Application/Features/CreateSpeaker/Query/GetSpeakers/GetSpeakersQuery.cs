using Domain.Dto;
using MediatR;

namespace Application.Features.CreateSpeaker.Query.GetSpeakers
{
    public class GetSpeakersQuery : IRequest<List<SpeakerDto>>
    {
    }
}

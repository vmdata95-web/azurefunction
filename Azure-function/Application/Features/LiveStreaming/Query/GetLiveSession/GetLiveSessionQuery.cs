using Domain.Dto;
using MediatR;
using System;

namespace Application.Features.LiveStreaming.Query.GetLiveSession
{
    public class GetLiveSessionQuery : IRequest<GetLiveSessionResponseDto>
    {
        public Guid SessionId { get; set; }
    }
}

using Domain.Dto;
using Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.LiveStreaming.Query.GetLiveSession
{
    public class GetLiveSessionQueryHandler : IRequestHandler<GetLiveSessionQuery, GetLiveSessionResponseDto>
    {
        private readonly ILiveStreamRepository _repository;

        public GetLiveSessionQueryHandler(ILiveStreamRepository repository)
        {
            _repository = repository;
        }

        public async Task<GetLiveSessionResponseDto> Handle(GetLiveSessionQuery request, CancellationToken cancellationToken)
        {
            var sessionExists = await _repository.SessionExistsAsync(request.SessionId, cancellationToken);
            if (!sessionExists)
            {
                // In a real application, you might throw a NotFoundException which is handled by a global error handler.
                // For simplicity here, we can return null or let the controller handle it if it returns null.
                return null;
            }

            var result = await _repository.GetLiveSessionDetailsAsync(request.SessionId, cancellationToken);
            
            return result;
        }
    }
}

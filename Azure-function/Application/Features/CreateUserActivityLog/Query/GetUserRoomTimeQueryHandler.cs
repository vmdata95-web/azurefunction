using Domain.Dto;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateUserActivityLog.Query
{
    public class GetUserRoomTimeQueryHandler
    : IRequestHandler<GetUserRoomTimeQuery, List<UserRoomTimeResponse>>
    {
        private readonly IUserActivityLogRepository _repository;

        public GetUserRoomTimeQueryHandler(
            IUserActivityLogRepository repository
        )
        {
            _repository = repository;
        }

        public async Task<List<UserRoomTimeResponse>> Handle(
            GetUserRoomTimeQuery request,
            CancellationToken cancellationToken
        )
        {
            return await _repository.GetUserRoomTimeAsync();
        }
    }
}

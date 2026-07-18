using Domain.Dto;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Query.GetUsers
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResponse<UserDto>>
    {
        private readonly IUserRepository _repository;

        public GetUsersQueryHandler(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResponse<UserDto>> Handle(
    GetUsersQuery request,
    CancellationToken cancellationToken)
        {
            return await _repository.GetUsersAsync(
                request.PageNumber,
                10,
                request.Email);
        }
    }
}

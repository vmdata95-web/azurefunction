using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Command.UpdateUserRole
{
    public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, string>
    {
        private readonly IUserRepository _repository;

        public UpdateUserRoleCommandHandler(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            await _repository.UpdateUserRoleAsync(
                request.Email,
                request.Role,
                request.Password);

            return "User role updated successfully.";
        }
    }
}

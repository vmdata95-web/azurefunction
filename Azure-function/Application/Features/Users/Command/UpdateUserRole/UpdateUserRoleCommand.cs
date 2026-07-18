using Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Command.UpdateUserRole
{
    public class UpdateUserRoleCommand : IRequest<string>
    {
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}

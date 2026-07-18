using Domain.Dto;
using MediatR;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Command.Userlogin
{
    public class LoginUserCommand : IRequest<LoginUserResponse>
    {
        public string Email { get; set; } = string.Empty;
        public string? Pass { get; set; }
    }


    
}

namespace Application.Features.Users.Command.Userlogin1
{

    public class LoginUserCommandlogin_PASS : IRequest<LoginUserResponse>
    {
        public string Email { get; set; } = string.Empty;
        public string? Pass { get; set; }
    }

}

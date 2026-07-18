using Domain.Dto;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// XAct.Core.PCL removed — package was unused; this import had no active references.

namespace Application.Features.Users.Query.GetUsers
{
    public class GetUsersQuery : IRequest<PagedResponse<UserDto>>
    {
        public int PageNumber { get; set; } = 1;
        public string? Email { get; set; }
    }
}

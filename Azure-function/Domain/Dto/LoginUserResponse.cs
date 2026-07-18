using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Dto
{
    public class LoginUserResponse
    {
        public string Token { get; set; } = string.Empty;

        public Guid UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        //public string? PhoneNumber { get; set; }

        public string? Role { get; set; }

        public Guid? EventId { get; set; }

        public string? Designation { get; set; }
        public string? CompanyName { get; set; }
        public string? MobileNo { get; set; }
        public string? Country { get; set; }
    }
}

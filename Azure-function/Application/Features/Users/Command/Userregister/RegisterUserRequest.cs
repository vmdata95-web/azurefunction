using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Command.Userregister
{
    public class RegisterUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? CompanyName { get; set; }
        public string? MobileNo { get; set; }
        public string? Country { get; set; }
        public string? Number_Of_Employees { get; set; }
        public int? registerfrom { get; set; }
        public string? ip_address { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Command.Userregister
{
    using MediatR;
    using System.Text.Json.Serialization;

    public class RegisterUserCommand : IRequest<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? CompanyName { get; set; }
        public string? MobileNo { get; set; }
        public string? Country { get; set; }

        [JsonPropertyName("registerfrom")]
        public string? NumberOfEmployees { get; set; }

        [JsonPropertyName("number_Of_Employees")]
        public int? RegisterFrom { get; set; }

        public string? IpAddress { get; set; }
    }
}

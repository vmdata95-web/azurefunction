using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Command.Userlogin
{
    public class LoginSettings
    {
        public List<string> AllowedLoginEmails { get; set; } = new();
    }
}

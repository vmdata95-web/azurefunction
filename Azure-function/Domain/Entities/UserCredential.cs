using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UserCredential
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Dto
{
    public class UserRoomTimeResponse
    {
        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string RoomName { get; set; } = string.Empty;

        public DateTime EnterTime { get; set; }

        public DateTime? OutTime { get; set; }

        public double TotalMinutes { get; set; }
    }
}

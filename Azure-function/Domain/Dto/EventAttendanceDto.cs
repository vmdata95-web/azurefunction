using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Dto
{
    public class EventAttendanceDto
    {
        public int TotalRegistered { get; set; }

        public int TotalAttended { get; set; }

        public int TotalNotAttended { get; set; }
    }
}

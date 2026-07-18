using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    /// <summary>
    /// Lifecycle states for an <see cref="Entities.EmailQueue"/> record.
    /// Stored as int in the database so queries like Status = 0 remain simple SQL.
    /// </summary>
    public enum EmailStatus
    {
        Pending = 0,
        Processing = 1,
        Sent = 2,
        Failed = 3
    }

}

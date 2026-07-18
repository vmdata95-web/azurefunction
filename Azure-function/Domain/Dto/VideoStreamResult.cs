using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Dto
{
    public class VideoStreamResult
    {
        public Stream? Stream { get; set; }
        public string? ContentType { get; set; }
        public bool IsAllowed { get; set; }
        public string? Message { get; set; }
    }
}

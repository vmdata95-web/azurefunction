using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Dto;

namespace Domain.Dto
{
    /// <summary>
    /// API response returned to the client after a live stream is successfully started.
    /// </summary>
    public class StartLiveStreamResponseDto
    {
        /// <summary>Always true on a 200/201 response.</summary>
        public bool   Success     { get; set; } = true;

        public string Message     { get; set; } = "Live stream started successfully.";

        /// <summary>The full live stream record that was created.</summary>
        public LiveStreamDto? Data { get; set; }
    }
}

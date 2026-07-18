using MediatR;
using System;
using System.IO;

namespace Application.Features.CreateSession.Query.GetPseudoLiveSegment
{
    public class GetPseudoLiveSegmentQuery : IRequest<Stream>
    {
        public Guid SessionId { get; set; }
        public int SegmentIndex { get; set; }

        /// <summary>
        /// The original blob file name for this segment (e.g. "segment001.ts").
        /// Populated from the <c>fileName</c> query-string parameter set by the
        /// playlist URL-rewriter.
        /// </summary>
        public string? FileName { get; set; }

        public GetPseudoLiveSegmentQuery(Guid sessionId, int segmentIndex, string? fileName = null)
        {
            SessionId = sessionId;
            SegmentIndex = segmentIndex;
            FileName = fileName;
        }
    }
}

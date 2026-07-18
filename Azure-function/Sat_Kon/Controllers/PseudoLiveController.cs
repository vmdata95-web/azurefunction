using Application.Features.CreateSession.Query.GetPseudoLivePlaylist;
using Application.Features.CreateSession.Query.GetPseudoLiveSegment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sat_Kon.Controllers
{
    [ApiController]
    [Route("api/Exhibitor/pseudo-live")]
    public class PseudoLiveController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PseudoLiveController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns a dynamically generated HLS Media Playlist (.m3u8) fetched from Azure Blob
        /// Storage under <c>session-live-video/{sessionId}/hlt_playlist/</c>.
        /// <para>
        ///   When the session has exactly one playlist it is served directly.
        ///   When the session has multiple playlists and no <paramref name="playlistName"/> is
        ///   supplied, a JSON array of available playlist names is returned instead so the client
        ///   can pick one and re-request.
        /// </para>
        /// </summary>
        /// <param name="sessionId">The GUID of the session whose HLS stream is requested.</param>
        /// <param name="playlistName">
        ///   Optional name of the specific playlist file (e.g. <c>playlist_720p.m3u8</c>).
        ///   Required when the session has more than one playlist.
        /// </param>
        [HttpGet("playlist/{sessionId:guid}")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        public async Task<IActionResult> GetPseudoLivePlaylist(
            [FromRoute] Guid sessionId,
            [FromQuery] string? playlistName,
            CancellationToken cancellationToken)
        {
            var scheme = Request.Scheme;
            var host = Request.Host.Value;
            var basePath = $"{scheme}://{host}/api/Exhibitor/pseudo-live";

            var result = await _mediator.Send(
                new GetPseudoLivePlaylistQuery(sessionId, basePath, playlistName),
                cancellationToken);

            if (!result.IsPlaylist)
            {
                // Multiple playlists exist — return the list as JSON for client selection
                return Ok(new { sessionId, playlists = result.AvailablePlaylists });
            }

            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return Content(result.PlaylistContent!, "application/vnd.apple.mpegurl", Encoding.UTF8);
        }

        /// <summary>
        /// Proxies a single HLS transport-stream (.ts) segment from Azure Blob Storage.
        /// Rejects the request with <c>403 Forbidden</c> if the segment index falls outside
        /// the current pseudo-live sliding window (replay-attack prevention).
        /// </summary>
        /// <param name="sessionId">The GUID of the session.</param>
        /// <param name="segmentIndex">Zero-based index of the requested .ts segment.</param>
        /// <param name="fileName">
        ///   The original blob file name appended by the playlist URL-rewriter
        ///   (e.g. <c>segment001.ts</c>). Used to locate the exact blob.
        /// </param>
        [HttpGet("segment/{sessionId:guid}/{segmentIndex:int}")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        public async Task<IActionResult> GetPseudoLiveSegment(
            [FromRoute] Guid sessionId,
            [FromRoute] int segmentIndex,
            [FromQuery] string? fileName,
            CancellationToken cancellationToken)
        {
            var stream = await _mediator.Send(
                new GetPseudoLiveSegmentQuery(sessionId, segmentIndex, fileName),
                cancellationToken);

            Response.Headers["Cache-Control"] = "no-store, no-cache";

            return File(stream, "video/mp2t", enableRangeProcessing: false);
        }
    }
}

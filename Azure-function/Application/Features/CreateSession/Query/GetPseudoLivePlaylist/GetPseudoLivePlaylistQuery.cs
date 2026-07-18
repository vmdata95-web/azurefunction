using MediatR;
using System;

namespace Application.Features.CreateSession.Query.GetPseudoLivePlaylist
{
    public class GetPseudoLivePlaylistQuery : IRequest<GetPseudoLivePlaylistResult>
    {
        public Guid SessionId { get; set; }
        public string BasePath { get; set; }
        public string? PlaylistName { get; set; }

        public GetPseudoLivePlaylistQuery(Guid sessionId, string basePath, string? playlistName = null)
        {
            SessionId = sessionId;
            BasePath = basePath;
            PlaylistName = playlistName;
        }
    }
}

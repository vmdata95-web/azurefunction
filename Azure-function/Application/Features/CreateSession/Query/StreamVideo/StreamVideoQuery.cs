using MediatR;

namespace Application.Features.CreateSession.Query.StreamVideo
{
    public class StreamVideoQuery : IRequest<(Stream stream, string contentType)>
    {
        public string FileName { get; set; } = string.Empty;
    }
}
using Application.Common.Exceptions;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class VideoRepository : IVideoRepository
    {
        private readonly AppDbContext _context;
        private readonly IBlobStorageService _blobStorageService;

        public VideoRepository(AppDbContext context, IBlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        public async Task<(Stream stream, string contentType)> GetVideoStreamAsync(
    string fileName)
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(x =>
                    x.VideoUrl != null &&
                    x.VideoUrl.EndsWith(fileName));

            if (session == null)
                throw new BadRequestException("Video not found");

            // Time validation
            var indiaTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            var currentIndianTime =
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone);

            if (currentIndianTime < session.StartTime)
            {
                throw new BadRequestException(
                    $"Video will be available at {session.StartTime:dd-MM-yyyy hh:mm tt}");
            }

            // Azure URL se blob path nikaalo
            var uri = new Uri(session.VideoUrl);

            var blobName = string.Join("/",
                uri.AbsolutePath
                   .TrimStart('/')
                   .Split('/')
                   .Skip(1));

            var stream = await _blobStorageService.GetBlobStreamAsync(blobName);

            if (stream == null)
                throw new BadRequestException("Video stream not found");

            var extension = Path.GetExtension(blobName).ToLower();

            var contentType = extension switch
            {
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".avi" => "video/x-msvideo",
                ".webm" => "video/webm",
                _ => "application/octet-stream"
            };

            return (stream, contentType);
        }
    }
}
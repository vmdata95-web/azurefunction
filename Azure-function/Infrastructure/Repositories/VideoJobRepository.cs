using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class VideoJobRepository : IVideoJobRepository
    {
        private readonly AppDbContext _context;

        public VideoJobRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(VideoJob videoJob)
        {
            await _context.VideoJobs.AddAsync(videoJob);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

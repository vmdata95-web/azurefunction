using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ExhibitorRepository : IExhibitorRepository
    {
        private readonly AppDbContext _context;

        public ExhibitorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Exhibitor exhibitor)
        {
            await _context.Exhibitors.AddAsync(exhibitor);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetByEventIdAsync(Guid eventId)
        {
            var exhibitors = await _context.Exhibitors
                .Where(x => x.EventId == eventId)
                .ToListAsync();

            var result = new List<string>();

            foreach (var exhibitor in exhibitors)
            {
                var videoJob = await _context.VideoJobs
                    .Where(v => v.ExhibitorId == exhibitor.Id)
                    .OrderByDescending(v => v.CreatedAt)
                    .FirstOrDefaultAsync();

                if (videoJob != null && videoJob.Status == "Ready" && !string.IsNullOrEmpty(videoJob.ManifestUrl))
                {
                    result.Add(videoJob.ManifestUrl);
                }
                else if (!string.IsNullOrEmpty(exhibitor.url))
                {
                    result.Add(exhibitor.url);
                }
            }

            return result;
        }

        public async Task<bool> HasExhibitorAsync()
        {
            return await _context.Exhibitors.AnyAsync();
        }
    }
}
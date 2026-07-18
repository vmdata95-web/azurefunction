using Domain.Dto;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
//using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Event entity)
        {
            await _context.Events.AddAsync(entity);
            await _context.SaveChangesAsync();
        }


        public async Task<string> UploadAsync(byte[] fileBytes, string fileName, string folderName)
        {
            // ✅ wwwroot path
            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var folderPath = Path.Combine(rootPath, folderName);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // ✅ unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            // ✅ save file
            await File.WriteAllBytesAsync(filePath, fileBytes);

            // ❗ IMPORTANT: return URL (not full path)
            return $"/{folderName}/{uniqueFileName}";
        }


        public async Task<List<EventDto>> GetEventsAsync(string? type)
        {
            var query = _context.Events.AsQueryable();

            // Filter only active events
            if (!string.IsNullOrWhiteSpace(type) &&
                type.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.IsActive == true);
            }

            return await query
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    BannerUrl = e.BannerUrl,
                    Status = e.Status
                })
                .ToListAsync();
        }



        public async Task<Event?> GetActiveEventAsync()
        {
            return await _context.Events
                .FirstOrDefaultAsync(x => x.IsActive == true);
        }


        public async Task AddUserEventAsync(UserEvent userEvent)
        {
            await _context.UserEvents.AddAsync(userEvent);
        }

        public async Task<bool> EventExistsAsync(Guid eventId)
        {
            return await _context.Events
                .AnyAsync(e => e.Id == eventId);
        }
    }
}

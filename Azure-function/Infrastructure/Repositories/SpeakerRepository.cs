using Domain.Dto;
using Domain.Entities;
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
    public class SpeakerRepository : ISpeakerRepository
    {
        private readonly AppDbContext _context;

        public SpeakerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> UserExistsAsync(Guid userId)
        {
            return await _context.Users.AnyAsync(x => x.Id == userId);
        }

        public async Task<bool> SpeakerExistsAsync(Guid userId)
        {
            return await _context.Speakers.AnyAsync(x => x.UserId == userId);
        }

        public async Task<Guid> CreateSpeakerAsync(Speaker speaker)
        {
            _context.Speakers.Add(speaker);
            await _context.SaveChangesAsync();
            return speaker.Id;
        }

        public async Task UpdateUserRoleAsync(Guid userId, string role)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Role = role;
                await _context.SaveChangesAsync();
            }
        }


        public async Task<List<SpeakerDto>> GetSpeakersAsync()
        {
            return await _context.Speakers
                .Include(s => s.User)
                .Select(s => new SpeakerDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    Name = s.User.Name,
                    Email = s.User.Email,
                    Bio = s.Bio,
                    Company = s.Company,
                    Website = s.Website,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> IsSpeakerForChatRoomAsync(
            Guid userId,
            Guid chatRoomId,
            CancellationToken cancellationToken = default)
        {
            // Resolves: is this userId a speaker for the event that owns the given chatRoom?
            // Join: ChatRoom → EventId → rooms in that event → sessions → speaker.UserId
            return await _context.ChatRooms
                .AsNoTracking()
                .Where(cr => cr.Id == chatRoomId)
                .Join(
                    _context.Sessions.Include(s => s.Speaker),
                    cr => cr.EventId,
                    s  => s.Room.Event.Id,
                    (cr, s) => s.Speaker.UserId)
                .AnyAsync(speakerUserId => speakerUserId == userId, cancellationToken);
        }
    }
}

using Domain.Dto;
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
    public class UserActivityLogRepository : IUserActivityLogRepository
    {
        private readonly AppDbContext _context;

        public UserActivityLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserActivityLog log)
        {
            await _context.UserActivityLogs.AddAsync(log);

            await _context.SaveChangesAsync();
        }

        public async Task<UserActivityLog?> GetLatestEnterLogAsync(
            Guid userId,
            Guid eventId
        )
        {
            return await _context.UserActivityLogs
                .Where(x =>
                    x.UserId == userId &&
                    x.EventId == eventId &&
                    x.Action == UserActivityAction.Enter
                )
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(UserActivityLog log)
        {
            _context.UserActivityLogs.Update(log);

            await _context.SaveChangesAsync();
        }

        public async Task<List<UserRoomTimeResponse>> GetUserRoomTimeAsync()
        {
            return await _context.UserActivityLogs
                .Where(x => x.Action == UserActivityAction.Out)
                .Select(x => new UserRoomTimeResponse
                {
                    UserName = x.User.Name,
                    Email = x.User.Email,
                    RoomName = x.RoomName,
                    EnterTime = x.CreatedAt,
                    OutTime = x.UpdatedAt,
                    TotalMinutes = x.UpdatedAt.HasValue
                        ? (x.UpdatedAt.Value - x.CreatedAt).TotalMinutes
                        : 0
                })
                .OrderByDescending(x => x.EnterTime)
                .ToListAsync();
        }

        public async Task<int> GetTotalAttendedAsync(Guid eventId)
        {
            return await _context.UserActivityLogs
                .Where(x =>
                    x.EventId == eventId &&
                    x.Action == UserActivityAction.EventJoined)
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync();
        }

        public async Task<int> GetTotalRegisteredAsync(Guid eventId)
        {
            return await _context.UserEvents
                .CountAsync(x => x.EventId == eventId);
        }

        public async Task<int> GetTotalNotAttendedAsync(Guid eventId)
        {
            var totalRegistered = await GetTotalRegisteredAsync(eventId);

            var totalAttended = await GetTotalAttendedAsync(eventId);

            return totalRegistered - totalAttended;
        }

        public async Task<int> GetLiveUsersAsync(Guid eventId)
        {
            return await _context.UserActivityLogs
                .Where(x =>
                    x.EventId == eventId &&
                    x.RoomName == "login" &&
                    x.Action == UserActivityAction.Enter)
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync();
        }
    }
}

using Domain.Dto;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IUserActivityLogRepository
    {
        Task AddAsync(UserActivityLog log);

        Task<UserActivityLog?> GetLatestEnterLogAsync(
            Guid userId,
            Guid eventId
        );

        Task UpdateAsync(UserActivityLog log);

        Task<List<UserRoomTimeResponse>> GetUserRoomTimeAsync();

        Task<int> GetTotalAttendedAsync(Guid eventId);

        Task<int> GetTotalRegisteredAsync(Guid eventId);

        Task<int> GetTotalNotAttendedAsync(Guid eventId);

        Task<int> GetLiveUsersAsync(Guid eventId);
    }
}

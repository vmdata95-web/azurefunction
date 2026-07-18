using Domain.Dto;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using XAct.Messages;


namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByEmailAndRegisterFromAsync(string email, Registerfrom registerFrom);
        Task AddAsync(User user);
        Task SaveChangesAsync();
        Task UpdateAsync(User user);
        Task<User?> GetByIdAsync(Guid id);

        Task<bool> IsUserAlreadyRegistered(Guid userId, Guid eventId);

        Task<Guid?> GetEventIdByUserIdAsync(Guid userId);

       Task<PagedResponse<UserDto>> GetUsersAsync(
    int pageNumber,
    int pageSize,
    string? email);

        Task UpdateUserRoleAsync(string email, UserRole role, string password);
    }
}

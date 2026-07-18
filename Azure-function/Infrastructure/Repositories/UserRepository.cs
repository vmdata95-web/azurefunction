using Application.Features.Users.Query.GetUsers;
using Domain.Dto;
using Domain.Entities;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
//using XAct.Messages;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(x => x.UserCredential)
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        //public async Task UpdateUserRoleAsync(string email, UserRole role, string password)
        //{
        //    var user = await _context.Users
        //        .Include(x => x.UserCredential)
        //        .FirstOrDefaultAsync(x => x.Email == email);

        //    if (user == null)
        //        throw new Exception("User not found.");

        //    if (user.RoleEnum != UserRole.User)
        //        throw new Exception("Only users with the User role can be updated.");

        //    user.RoleEnum = role;

        //    // Password Hash
        //    var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        //    if (user.UserCredential == null)
        //    {
        //        user.UserCredential = new UserCredential
        //        {
        //            UserId = user.Id,
        //            PasswordHash = passwordHash
        //        };
        //    }
        //    else
        //    {
        //        user.UserCredential.PasswordHash = passwordHash;
        //    }

        //    await _context.SaveChangesAsync();
        //}


        public async Task UpdateUserRoleAsync(string email, UserRole role, string password)
        {
            var user = await _context.Users
                .Include(x => x.UserCredential)
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
                throw new Exception("User not found.");

            if (user.RoleEnum != UserRole.User)
                throw new Exception("Only users with the User role can be updated.");

            user.RoleEnum = role;

            // Password Hash
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            if (user.UserCredential == null)
            {
                user.UserCredential = new UserCredential
                {
                    UserId = user.Id,
                    PasswordHash = passwordHash
                };
            }
            else
            {
                user.UserCredential.PasswordHash = passwordHash;
            }

            
            if (role == UserRole.Speaker)
            {
                bool speakerExists = await _context.Speakers
                    .AnyAsync(s => s.UserId == user.Id);

                if (!speakerExists)
                {
                    _context.Speakers.Add(new Speaker
                    {
                        UserId = user.Id,
                        Bio = string.Empty,
                        Company = string.Empty,
                        Website = string.Empty,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }



        public async Task<User?> GetByEmailAndRegisterFromAsync(
    string email,
    Registerfrom registerFrom)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email == email &&
                    x.Registerfrom == (int)registerFrom);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<bool> IsUserAlreadyRegistered(Guid userId, Guid eventId)
        {
            return await _context.UserEvents
                .AnyAsync(x => x.UserId == userId && x.EventId == eventId);
        }

        public async Task<Guid?> GetEventIdByUserIdAsync(Guid userId)
        {
            var userEvent = await _context.UserEvents
                .Where(x => x.UserId == userId && x.EventId != null)
                .OrderByDescending(x => x.RegisteredAt)
                .FirstOrDefaultAsync();

            return userEvent?.EventId;
        }

        public async Task<PagedResponse<UserDto>> GetUsersAsync(
            int pageNumber,
            int pageSize,
            Guid? id,
            string? name)
        {
            var query = _context.Users.AsQueryable();

            if (id.HasValue)
                query = query.Where(x => x.Id == id.Value);

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(x => x.Name.Contains(name));

            var totalRecords = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Email = x.Email,
                    //Role = x.Role,
                    //IsActive = x.IsActive,
                    //CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return new PagedResponse<UserDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                Data = data
            };
        }


        public async Task<PagedResponse<UserDto>> GetUsersAsync(
    int pageNumber,
    int pageSize,
    string? email)
        {
            var query = _context.Users.AsQueryable();

            // 🔥 Fixed Role filter (always applied)
            query = query.Where(x => x.Role == "User");

            // Email filter (optional)
            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(x => x.Email.Contains(email));
            }

            var totalRecords = await query.CountAsync();

            var data = await query
                .OrderBy(x => x.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Email = x.Email,
                    //Role = x.Role
                })
                .ToListAsync();

            return new PagedResponse<UserDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                Data = data
            };
        }


    }
    }

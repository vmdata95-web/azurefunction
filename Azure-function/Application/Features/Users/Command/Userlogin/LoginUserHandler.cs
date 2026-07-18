//using Application.Common.Exceptions;
//using Domain.Dto;
//using Domain.Entities;
//using Domain.Enums;
//using Domain.Interfaces;
//using Domain.Interfaces;
//using MediatR;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Application.Features.Users.Command.Userlogin
//{
//    public class LoginUserHandler : IRequestHandler<LoginUserCommand, LoginUserResponse>
//    {
//        private readonly IUserRepository _repo;
//        private readonly IJwtTokenService _jwt;
//        private readonly IUserActivityLogRepository _activityRepo;

//        private readonly IEventRepository _get_event;

//        public LoginUserHandler(IUserActivityLogRepository activityRepo,IUserRepository repo, IJwtTokenService jwt,IEventRepository get_event)
//        {
//            _repo = repo;
//            _jwt = jwt;
//            _activityRepo = activityRepo;
//            _get_event = get_event;
//        }

//        public async Task<LoginUserResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
//        {
//            var user = await _repo.GetByEmailAsync(request.Email);

//            if (user == null)
//                throw new BadRequestException(
//                    "Login failed. Please try again with your registered email.");

//            // Password check only if provided
//            if (!string.IsNullOrEmpty(request.Pass))
//            {
//                if (user.UserCredential == null ||
//     string.IsNullOrEmpty(user.UserCredential.PasswordHash))
//                {
//                    throw new BadRequestException(
//                        "Login failed. Please check your email and password.");
//                }

//                bool isValid = BCrypt.Net.BCrypt.Verify(
//                    request.Pass,
//                    user.UserCredential.PasswordHash
//                );

//                if (!isValid)
//                    throw new BadRequestException("Invalid password");
//            }

//            await _repo.UpdateAsync(user);

//            // Get Active Event
//            var activeEvent = await _get_event.GetActiveEventAsync();

//            if (activeEvent != null)
//            {
//                var log = new UserActivityLog
//                {
//                    Id = Guid.NewGuid(),
//                    UserId = user.Id,
//                    EventId = activeEvent.Id,
//                    Action = UserActivityAction.EventJoined,
//                    Metadata = "{}",
//                    RoomName = "event_att",
//                    CreatedAt = DateTime.UtcNow
//                };

//                await _activityRepo.AddAsync(log);
//            }

//            // Generate JWT
//            var tokenResult = _jwt.GenerateToken(user);

//            // Return Response (ONLY active event)
//            return new LoginUserResponse
//            {
//                Token = tokenResult.AccessToken,
//                UserId = user.Id,
//                Name = user.Name,
//                Email = user.Email,
//                Role = user.Role,
//                EventId = activeEvent?.Id, 
//                Designation = user.Designation,
//                CompanyName = user.CompanyName,
//                MobileNo = user.MobileNo,
//                Country = user.Country
//            };
//        }
//    }
//}
using Application.Common.Exceptions;
using Application.Features.Users.Command.Userlogin;
using Application.Features.Users.Command.Userlogin1;
using Domain.Dto;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Users.Command.Userlogin
{
    public class LoginUserHandler : IRequestHandler<LoginUserCommand, LoginUserResponse>
    {
        private readonly IUserRepository _repo;
        private readonly IJwtTokenService _jwt;
        private readonly IUserActivityLogRepository _activityRepo;
        private readonly IEventRepository _get_event;
        private readonly LoginSettings _loginSettings;

        public LoginUserHandler(
            IUserActivityLogRepository activityRepo,
            IUserRepository repo,
            IJwtTokenService jwt,
            IEventRepository get_event,
            IOptions<LoginSettings> loginSettings)
        {
            _repo = repo;
            _jwt = jwt;
            _activityRepo = activityRepo;
            _get_event = get_event;
            _loginSettings = loginSettings.Value;
        }

        public async Task<LoginUserResponse> Handle(
            LoginUserCommand request,
            CancellationToken cancellationToken)
        {
            // Allow only emails configured in appsettings.json
            var allowedEmails = _loginSettings.AllowedLoginEmails ?? new List<string>();

            if (!allowedEmails.Any(x =>
                    x.Trim().Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BadRequestException("You are not authorized to login.");
            }

            var user = await _repo.GetByEmailAsync(request.Email);

            if (user == null)
            {
                throw new BadRequestException(
                    "Login failed. Please try again with your registered email.");
            }

            // Password check only if provided
            if (!string.IsNullOrEmpty(request.Pass))
            {
                if (user.UserCredential == null ||
                    string.IsNullOrEmpty(user.UserCredential.PasswordHash))
                {
                    throw new BadRequestException(
                        "Login failed. Please check your email and password.");
                }

                bool isValid = BCrypt.Net.BCrypt.Verify(
                    request.Pass,
                    user.UserCredential.PasswordHash);

                if (!isValid)
                {
                    throw new BadRequestException("Invalid password");
                }
            }

            // Normalize stored role using enum; repair invalid values and persist
            var normalizedRole = Enum.TryParse<Domain.Enums.UserRole>(user.Role, true, out var roleEnum)
                ? roleEnum.ToString()
                : Domain.Enums.UserRole.User.ToString();

            if (!string.Equals(normalizedRole, user.Role, StringComparison.OrdinalIgnoreCase))
            {
                user.Role = normalizedRole;
            }

            await _repo.UpdateAsync(user);

            // Get Active Event
            var activeEvent = await _get_event.GetActiveEventAsync();

            if (activeEvent != null)
            {
                var log = new UserActivityLog
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    EventId = activeEvent.Id,
                    Action = UserActivityAction.EventJoined,
                    Metadata = "{}",
                    RoomName = "event_att",
                    CreatedAt = DateTime.UtcNow
                };

                await _activityRepo.AddAsync(log);
            }

            user.Role = UserRole.User.ToString();

            // Generate JWT
            var tokenResult = _jwt.GenerateToken(user);

            return new LoginUserResponse
            {
                Token = tokenResult.AccessToken,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = UserRole.User.ToString(),
                EventId = activeEvent?.Id,
                Designation = user.Designation,
                CompanyName = user.CompanyName,
                MobileNo = user.MobileNo,
                Country = user.Country
            };
        }
    }

    
    
    }

namespace Application.Features.Users.Command.Userlogin1
{

    public class LoginUserHandlerlogin_PASS : IRequestHandler<LoginUserCommandlogin_PASS, LoginUserResponse>
    {
        private readonly IUserRepository _repo;
        private readonly IJwtTokenService _jwt;
        private readonly IUserActivityLogRepository _activityRepo;
        private readonly IEventRepository _get_event;
        private readonly LoginSettings _loginSettings;

        public LoginUserHandlerlogin_PASS(
            IUserActivityLogRepository activityRepo,
            IUserRepository repo,
            IJwtTokenService jwt,
            IEventRepository get_event,
            IOptions<LoginSettings> loginSettings)
        {
            _repo = repo;
            _jwt = jwt;
            _activityRepo = activityRepo;
            _get_event = get_event;
            _loginSettings = loginSettings.Value;
        }

        public async Task<LoginUserResponse> Handle(
            LoginUserCommandlogin_PASS request,
            CancellationToken cancellationToken)
        {
            // Allow only emails configured in appsettings.json
            var allowedEmails = _loginSettings.AllowedLoginEmails ?? new List<string>();

            if (!allowedEmails.Any(x =>
                    x.Trim().Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BadRequestException("You are not authorized to login.");
            }

            var user = await _repo.GetByEmailAsync(request.Email);

            if (user == null)
            {
                throw new BadRequestException(
                    "Login failed. Please try again with your registered email.");
            }

            // Password check only if provided
            if (!string.IsNullOrEmpty(request.Pass))
            {
                if (user.UserCredential == null ||
                    string.IsNullOrEmpty(user.UserCredential.PasswordHash))
                {
                    throw new BadRequestException(
                        "Login failed. Please check your email and password.");
                }

                bool isValid = BCrypt.Net.BCrypt.Verify(
                    request.Pass,
                    user.UserCredential.PasswordHash);

                if (!isValid)
                {
                    throw new BadRequestException("Invalid password");
                }
            }

            // Normalize stored role using enum; repair invalid values and persist
            var normalizedRole = Enum.TryParse<Domain.Enums.UserRole>(user.Role, true, out var roleEnum)
                ? roleEnum.ToString()
                : Domain.Enums.UserRole.User.ToString();

            if (!string.Equals(normalizedRole, user.Role, StringComparison.OrdinalIgnoreCase))
            {
                user.Role = normalizedRole;
            }

            await _repo.UpdateAsync(user);

            // Get Active Event
            var activeEvent = await _get_event.GetActiveEventAsync();

            if (activeEvent != null)
            {
                var log = new UserActivityLog
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    EventId = activeEvent.Id,
                    Action = UserActivityAction.EventJoined,
                    Metadata = "{}",
                    RoomName = "event_att",
                    CreatedAt = DateTime.UtcNow
                };

                await _activityRepo.AddAsync(log);
            }

            // Generate JWT
            var tokenResult = _jwt.GenerateToken(user);

            if (user.Role == "Speaker" ||
                user.Role == "Admin" ||
                user.Role == "SuperAdmin")
            {
                return new LoginUserResponse
                {
                    Token = tokenResult.AccessToken,
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role,
                    EventId = activeEvent?.Id,
                    Designation = user.Designation,
                    CompanyName = user.CompanyName,
                    MobileNo = user.MobileNo,
                    Country = user.Country
                };
            }
            else
            {
                throw new BadRequestException("You are not authorized to login.");
            }
        }
        }
}

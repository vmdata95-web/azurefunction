using Application.Features.Users.Command;
using Application.Features.Users.Command.UpdateUserRole;
using Application.Features.Users.Command.Userlogin;
using Application.Features.Users.Command.Userlogin1;
using Application.Features.Users.Command.Userregister;
using Application.Features.Users.Query.GetEventAttendance;
using Application.Features.Users.Query.GetLiveUsers;
using Application.Features.Users.Query.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserRequest request)
        {
            var command = new RegisterUserCommand
            {
                Name = request.Name,
                Email = request.Email,
                Designation = request.Designation,
                CompanyName = request.CompanyName,
                MobileNo = request.MobileNo,
                Country = request.Country,
                NumberOfEmployees = request.Number_Of_Employees,
                RegisterFrom = request.registerfrom,
                IpAddress = request.ip_address
            };

            var userId = await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                userId = userId
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserRequest request)
        {
            var command = new LoginUserCommand
            {
                Email = request.Email,
            };

            var response = await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                token = response.Token,
                userId = response.UserId,
                name = response.Name,
                email = response.Email,
                EventId = response.EventId,
                role = response.Role,
                designation = response.Designation,
                companyName = response.CompanyName,
                mobileNo = response.MobileNo,
                country = response.Country
            });
        }


        [HttpPost("login_PASS")]
        public async Task<IActionResult> PasswordLogin(LoginUserRequest1 request)
        {
            var command = new LoginUserCommandlogin_PASS
            {
                Email = request.Email,
                Pass = request.Pass
            };

            var response = await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                token = response.Token,
                userId = response.UserId,
                name = response.Name,
                email = response.Email,
                EventId = response.EventId,
                role = response.Role,
                designation = response.Designation,
                companyName = response.CompanyName,
                mobileNo = response.MobileNo,
                country = response.Country
            });
        }

        [HttpGet("get-user")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetUsers(
    [FromQuery] int pageNumber = 1,
    [FromQuery] string? email = null)
        {
            var result = await _mediator.Send(new GetUsersQuery
            {
                PageNumber = pageNumber,
                Email = email
            });

            return Ok(result);
        }

        [HttpPut("update-user-role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateUserRole(UpdateUserRoleCommand command)
        {
            var result = await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = result
            });
        }

        [HttpGet("attendance/{eventId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAttendance(Guid eventId)
        {
            var result = await _mediator.Send(
                new GetEventAttendanceQuery(eventId));

            return Ok(result);
        }

        [HttpGet("live-users/{eventId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLiveUsers(Guid eventId)
        {
            var result = await _mediator.Send(
                new GetLiveUsersQuery(eventId));

            return Ok(new
            {
                liveUsers = result
            });
        }

    }
}
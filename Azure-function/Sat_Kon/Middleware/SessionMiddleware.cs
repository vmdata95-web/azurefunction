using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Domain.Enums;

namespace Sat_Kon.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionMiddleware> _logger;

        public SessionMiddleware(RequestDelegate next, ILogger<SessionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, IUserRepository repo)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("Missing sub claim. Path: {Path}", context.Request.Path);
                    await Unauthorized(context, "Invalid token claims.");
                    return;
                }

                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    await Unauthorized(context, "Invalid user ID.");
                    return;
                }

                var user = await repo.GetByIdAsync(userId);

                if (user == null)
                {
                    await Unauthorized(context, "User not found.");
                    return;
                }

                if (!user.IsActive)
                {
                    await Forbidden(context, "User account is disabled.");
                    return;
                }

                // validate role value against enum to prevent invalid role injection
                if (!Enum.TryParse<UserRole>(user.Role, true, out var _))
                {
                    _logger.LogWarning("Invalid role for user {UserId}: {Role}", user.Id, user.Role);
                    await Forbidden(context, "Invalid role assignment.");
                    return;
                }

                // Attach the user to HttpContext for downstream handlers
                context.Items["User"] = user;

                _logger.LogInformation("Session Validation Succeeded for User: {UserId}", userId);
            }

            await _next(context);
        }

        private async Task Unauthorized(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new { success = false, message });
        }

        private async Task Forbidden(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new { success = false, message });
        }
    }
}

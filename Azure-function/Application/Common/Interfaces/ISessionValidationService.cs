using Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface ISessionValidationService
    {
        /// <summary>
        /// Validates that a session exists and is active for streaming/playlist access.
        /// Throws Domain exceptions (NotFoundException / ForbiddenException) on failure.
        /// Returns the Session entity when validation succeeds.
        /// </summary>
        Task<Session> ValidateSessionActiveAsync(Guid sessionId, string caller, CancellationToken cancellationToken = default);
    }
}

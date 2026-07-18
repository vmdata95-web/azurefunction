using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Services
{
    public class SessionValidationService : ISessionValidationService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly Application.Common.Interfaces.ITimeProvider _timeProvider;
        private readonly ILogger<SessionValidationService> _logger;

        public SessionValidationService(
            ISessionRepository sessionRepository,
            Application.Common.Interfaces.ITimeProvider timeProvider,
            ILogger<SessionValidationService> logger)
        {
            _sessionRepository = sessionRepository;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<Session> ValidateSessionActiveAsync(Guid sessionId, string caller, CancellationToken cancellationToken = default)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null)
            {
                _logger.LogWarning("[{Caller}] Session {SessionId} not found.", caller, sessionId);
                throw new Domain.Exceptions.NotFoundException("Session not found.");
            }

            var now = _timeProvider.GetLocalTime();
            if (session.StartTime > now || session.EndTime <= now)
            {
                _logger.LogInformation(
    "Start={Start}, End={End}, Now={Now}",
    session.StartTime,
    session.EndTime,
    now);
                throw new Domain.Exceptions.ForbiddenException("Session is not active.");
            }

            return session;
        }
    }
}

using Application.Common.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateUserActivityLog.command
{
    public class CreateUserActivityLogCommandHandler
    : IRequestHandler<CreateUserActivityLogCommand, Guid>
    {
        private readonly IUserActivityLogRepository _repository;

        public CreateUserActivityLogCommandHandler(
            IUserActivityLogRepository repository
        )
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(
            CreateUserActivityLogCommand request,
            CancellationToken cancellationToken
        )
        {
            // ENTER
            if (request.Action == UserActivityAction.Enter)
            {
                var log = new UserActivityLog
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    EventId = request.EventId,
                    Action = UserActivityAction.Enter,
                    RoomName = request.RoomName,
                    Metadata = request.Metadata,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.AddAsync(log);

                return log.Id;
            }

            // OUT
            var existingLog = await _repository.GetLatestEnterLogAsync(
                request.UserId,
                request.EventId
            );

            if (existingLog == null)
            {
                throw new BadRequestException("No active enter log found");
            }

            existingLog.Action = UserActivityAction.Out;

            existingLog.RoomName = request.RoomName;

            existingLog.Metadata = request.Metadata;

            existingLog.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingLog);

            return existingLog.Id;
        }
    }
}

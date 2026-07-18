using Application.Common.Exceptions;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateSpeaker.Command.CreateSpeaker
{
    public class CreateSpeakerCommandHandler : IRequestHandler<CreateSpeakerCommand, Guid>
    {
        private readonly ISpeakerRepository _repository;

        public CreateSpeakerCommandHandler(ISpeakerRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(CreateSpeakerCommand request, CancellationToken cancellationToken)
        {
            // ✅ Step 1: User Exists?
            var userExists = await _repository.UserExistsAsync(request.UserId);
            if (!userExists)
                throw new BadRequestException("User not found");

            // ✅ Step 2: Already Speaker?
            var alreadySpeaker = await _repository.SpeakerExistsAsync(request.UserId);
            if (alreadySpeaker)
                throw new BadRequestException("Speaker already exists for this user");

            // ✅ Step 3: Create Speaker
            var speaker = new Speaker
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Bio = request.Bio,
                Company = request.Company,
                Website = request.Website,
                CreatedAt = DateTime.UtcNow
            };

            var speakerId = await _repository.CreateSpeakerAsync(speaker);

            // 🔥 Optional (Recommended)
            await _repository.UpdateUserRoleAsync(request.UserId, "speaker");

            return speakerId;
        }
    }
}

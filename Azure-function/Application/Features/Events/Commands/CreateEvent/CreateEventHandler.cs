using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Features.Events.Commands.CreateEvent
{

    public class CreateEventHandler : IRequestHandler<CreateEventCommand, Guid>
    {
        private readonly IEventRepository _repo;


        public CreateEventHandler(IEventRepository repo)
        {
            _repo = repo;
        }

        public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            string imageUrl = string.Empty;

            if (request.Banner != null && request.Banner.Length > 0)
            {
                using var ms = new MemoryStream();
                await request.Banner.CopyToAsync(ms, cancellationToken);

                var bytes = ms.ToArray();

                imageUrl = await _repo.UploadAsync(
                    bytes,
                    request.Banner.FileName,
                    "uploads"
                );
            }

            var entity = new Event
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Status = request.Status,
                BannerUrl = imageUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entity);

            return entity.Id;
        }
    }
}
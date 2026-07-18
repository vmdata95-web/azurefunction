using Domain.Entities;

namespace Domain.Interfaces
{
    /// <summary>
    /// Persistence contract for <see cref="RoomImage"/> entities.
    /// All infrastructure details (EF Core, SQL Server) are hidden behind this interface
    /// so that Application handlers stay free of any infrastructure dependency.
    /// </summary>
    public interface IRoomImageRepository
    {
        /// <summary>
        /// Checks whether a Room with the given <paramref name="roomId"/> exists in the database.
        /// Used to validate the FK before persisting a new image.
        /// </summary>
        Task<bool> RoomExistsAsync(Guid roomId, CancellationToken cancellationToken = default);

        /// <summary>Persists a new <see cref="RoomImage"/> record (does NOT call SaveChanges).</summary>
        Task AddAsync(RoomImage image, CancellationToken cancellationToken = default);

        /// <summary>Flushes pending changes to the database.</summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns a single image by its unique identifier, or <c>null</c> if not found.</summary>
        Task<RoomImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Returns all images that belong to the specified room.</summary>
        Task<List<RoomImage>> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns all images for the Room whose <c>Name</c> matches
        /// <paramref name="roomName"/> (case-insensitive), or an empty list if no match.
        /// </summary>
        Task<List<RoomImage>> GetByRoomNameAsync(string roomName, CancellationToken cancellationToken = default);
    }
}

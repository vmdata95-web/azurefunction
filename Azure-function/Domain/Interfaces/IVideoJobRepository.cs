using Domain.Entities;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IVideoJobRepository
    {
        Task AddAsync(VideoJob videoJob);
        Task SaveChangesAsync();
    }
}

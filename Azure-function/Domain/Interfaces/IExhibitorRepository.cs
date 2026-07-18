using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IExhibitorRepository
    {
        Task AddAsync(Exhibitor exhibitor);
        Task SaveChangesAsync();
        Task<List<string>> GetByEventIdAsync(Guid eventId);


        Task<bool> HasExhibitorAsync();


    }
}

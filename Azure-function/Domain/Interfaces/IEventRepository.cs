using Domain.Dto;
using Domain.Entities;
// Microsoft.AspNetCore.Http removed — no ASP.NET Core types used in this interface.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IEventRepository
    {
        Task AddAsync(Event entity);

        Task<string> UploadAsync(byte[] fileBytes, string fileName, string folderName);

        Task<List<EventDto>> GetEventsAsync(string type);

        Task AddUserEventAsync(UserEvent userEvent);

        Task<bool> EventExistsAsync(Guid eventId);

        Task<Event?> GetActiveEventAsync();


    }
}

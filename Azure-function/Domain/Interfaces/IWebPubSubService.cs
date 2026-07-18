using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IWebPubSubService
    {
        Task SendSessionStatusChangedAsync(
            Guid roomId,
            Guid sessionId,
            string title,
            string status,
            CancellationToken cancellationToken = default);

        Task SendToUserAsync(
            Guid userId,
            string eventName,
            object payload,
            CancellationToken cancellationToken = default);

        Task SendToGroupAsync(
            Guid sessionId,
            string eventName,
            object payload,
            CancellationToken cancellationToken = default);

        Task<Uri> GenerateClientAccessUriAsync(
            Guid userId,
            IEnumerable<string> groups,
            TimeSpan? tokenLifetime = null,
            CancellationToken cancellationToken = default);
    }
}

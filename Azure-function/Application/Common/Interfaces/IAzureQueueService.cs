namespace Application.Common.Interfaces
{
    /// <summary>
    /// Abstraction over Azure Storage Queue.
    /// Allows Application handlers to enqueue messages without taking a
    /// direct dependency on the Azure SDK or any infrastructure concern.
    /// </summary>
    public interface IAzureQueueService
    {
        /// <summary>
        /// Serialises <typeparamref name="T"/> to JSON, Base64-encodes it
        /// (required by the Azure Queue SDK), and enqueues it onto the
        /// configured Azure Storage Queue.
        /// </summary>
        /// <typeparam name="T">Any JSON-serialisable message type.</typeparam>  D:\Salesforceevent\Salesforceevent\Backend\Satkon-backend-git\Satkon-backend\Sat_Kon\Application\Common\Interfaces\IAzureQueueService.cs
        /// <param name="message">The message payload to enqueue.</param>
        /// <param name="cancellationToken">Propagates cancellation.</param>
        Task EnqueueAsync<T>(T message, CancellationToken cancellationToken = default);
    }
}

namespace Application.DTOs
{
    /// <summary>
    /// Payload that is serialised to JSON and pushed onto the Azure Storage
    /// Queue by <see cref="Features.Users.Command.Userregister.RegisterUserHandler"/>.
    ///
    /// A downstream consumer (e.g. an Azure Function or a BackgroundService)
    /// dequeues and processes this message to dispatch the welcome email. D:\Salesforceevent\Salesforceevent\Backend\Satkon-backend-git\Satkon-backend\Sat_Kon\Application\DTOs\EmailQueueMessage.cs
    /// </summary>
    public sealed class EmailQueueMessage
    {
        /// <summary>The database identifier of the registered user.</summary>
        public Guid UserId { get; init; }

        /// <summary>Recipient email address.</summary>
        public string Email { get; init; } = string.Empty;

        /// <summary>Email subject line.</summary>
        public string Subject { get; init; } = string.Empty;

        /// <summary>Full email body — HTML or plain-text depending on <see cref="IsHtml"/>.</summary>
        public string Body { get; init; } = string.Empty;

        /// <summary>
        /// <c>true</c> when <see cref="Body"/> contains HTML markup;
        /// <c>false</c> for plain-text.
        /// </summary>
        public bool IsHtml { get; init; }

        /// <summary>UTC timestamp at which the message was created.</summary>
        public DateTime CreatedAt { get; init; }
    }
}

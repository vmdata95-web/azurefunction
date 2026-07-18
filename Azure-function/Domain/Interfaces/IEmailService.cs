using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    /// <summary>
    /// Abstracts SMTP delivery so that:
    ///   • Application handlers never touch infrastructure packages.
    ///   • Tests can inject a no-op stub.
    ///   • The concrete implementation can be swapped (MailKit → SendGrid, etc.)
    ///     without touching business logic.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a single email.
        /// </summary>
        /// <param name="toEmail">Recipient email address.</param>
        /// <param name="subject">Email subject line.</param>
        /// <param name="body">Email body text or HTML.</param>
        /// <param name="isHtml">
        ///   True to send as text/html; false to send as text/plain.
        /// </param>
        /// <param name="cancellationToken">Propagates request/host cancellation.</param>
        /// <exception cref="InvalidOperationException">
        ///   Thrown when the SMTP connection or authentication fails.
        /// </exception>
        Task SendAsync(
            string toEmail,
            string subject,
            string body,
            bool isHtml = true,
            CancellationToken cancellationToken = default);
    }

}

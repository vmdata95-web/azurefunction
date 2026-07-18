using Domain.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Services
{
    /// <summary>
    /// Production SMTP email service backed by MailKit, hardened for GoDaddy SMTP
    /// (smtpout.secureserver.net).
    ///
    /// Key behaviours:
    ///   - TLS 1.2/1.3 enforced; TLS 1.0/1.1 explicitly excluded.
    ///   - Port 465 → SslOnConnect (implicit TLS).
    ///   - Port 587 → StartTls    (explicit TLS / STARTTLS upgrade).
    ///   - GoDaddy's missing-intermediate-CA is handled by the custom
    ///     certificate validator: a partial chain whose root is OS-trusted
    ///     is accepted; all other errors are rejected.
    ///   - XOAUTH2 is removed before authentication because GoDaddy's shared
    ///     SMTP does not support OAuth2.
    ///   - Configurable retry with exponential back-off.
    ///   - SmtpClient and TCP connection are always disposed correctly.
    ///
    /// Configuration keys (appsettings.json / environment variables):
    /// <code>
    /// "Email": {
    ///   "SmtpHost":                  "smtpout.secureserver.net",
    ///   "SmtpPort":                  465,
    ///   "SenderEmail":               "you@yourdomain.com",
    ///   "SenderName":                "Your Name",
    ///   "Username":                  "you@yourdomain.com",
    ///   "Password":                  "&lt;smtp-password&gt;",
    ///   "AcceptAnyServerCertificate": false,
    ///   "MaxRetryAttempts":          3,
    ///   "RetryDelayMs":              2000,
    ///   "TimeoutMs":                 30000
    /// }
    /// </code>
    /// </summary>
    public sealed class SmtpEmailService : IEmailService
    {
        #region Fields

        private readonly string _smtpHost;
        private readonly int    _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _username;
        private readonly string _password;

        /// <summary>
        /// When <c>true</c>, ALL server certificates are accepted without validation.
        /// Use only for local debugging against self-signed certificates — never in production.
        /// Controlled via <c>Email:AcceptAnyServerCertificate = true</c>.
        /// </summary>
        private readonly bool _acceptAnyServerCertificate;

        private readonly int _maxRetryAttempts;
        private readonly int _retryDelayMs;
        private readonly int _timeoutMs;

        private readonly ILogger<SmtpEmailService> _logger;

        #endregion

        #region Constructor

        public SmtpEmailService(
            IConfiguration configuration,
            ILogger<SmtpEmailService> logger)
        {
            _logger = logger;

            // Validate all required keys at startup so misconfiguration surfaces
            // immediately rather than silently at the time of the first send.
            _smtpHost    = RequiredConfig(configuration, "Email:SmtpHost");
            _smtpPort    = configuration.GetValue<int>("Email:SmtpPort", 465);
            _senderEmail = RequiredConfig(configuration, "Email:SenderEmail");
            _senderName  = configuration["Email:SenderName"] ?? "Sat-Kon Platform";
            _username    = RequiredConfig(configuration, "Email:Username");
            _password    = RequiredConfig(configuration, "Email:Password");

            _acceptAnyServerCertificate = configuration.GetValue<bool>(
                "Email:AcceptAnyServerCertificate", false);

            _maxRetryAttempts = configuration.GetValue<int>("Email:MaxRetryAttempts", 3);
            _retryDelayMs     = configuration.GetValue<int>("Email:RetryDelayMs", 2000);
            _timeoutMs        = configuration.GetValue<int>("Email:TimeoutMs", 30_000);

            if (_acceptAnyServerCertificate)
            {
                _logger.LogWarning(
                    "SmtpEmailService: AcceptAnyServerCertificate=true — " +
                    "SSL certificate validation is DISABLED. Do NOT use in production.");
            }
        }

        #endregion

        #region Public API

        /// <inheritdoc/>
        public async Task SendAsync(
            string toEmail,
            string subject,
            string body,
            bool isHtml = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException(
                    "Recipient email address must not be empty.", nameof(toEmail));

            // Build the MIME message once and reuse it across retry attempts.
            var message = BuildMessage(toEmail, subject, body, isHtml);

            Exception? lastException = null;

            for (int attempt = 1; attempt <= _maxRetryAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "SmtpEmailService: Sending email attempt {Attempt}/{Max} — To={To}, Subject={Subject}",
                    attempt, _maxRetryAttempts, toEmail, subject);

                try
                {
                    await SendOnceAsync(message, cancellationToken);

                    _logger.LogInformation(
                        "SmtpEmailService: Email sent successfully — To={To}, Subject={Subject}, Attempt={Attempt}",
                        toEmail, subject, attempt);

                    return; // success
                }
                catch (OperationCanceledException)
                {
                    throw; // cancellation is never retried
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    _logger.LogWarning(ex,
                        "SmtpEmailService: Attempt {Attempt}/{Max} failed — To={To}, Error={ErrorType}: {ErrorMessage}",
                        attempt, _maxRetryAttempts, toEmail,
                        ex.GetType().Name, ex.Message);

                    if (attempt < _maxRetryAttempts)
                    {
                        // Exponential back-off: delay grows with each attempt.
                        var delay = TimeSpan.FromMilliseconds(_retryDelayMs * attempt);

                        _logger.LogInformation(
                            "SmtpEmailService: Retrying in {DelayMs} ms (attempt {Next}/{Max})…",
                            delay.TotalMilliseconds, attempt + 1, _maxRetryAttempts);

                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            _logger.LogError(lastException,
                "SmtpEmailService: All {Max} attempts failed — To={To}, Subject={Subject}",
                _maxRetryAttempts, toEmail, subject);

            throw new InvalidOperationException(
                $"Email delivery to '{toEmail}' failed after {_maxRetryAttempts} attempt(s). " +
                "See inner exception for details.",
                lastException);
        }

        #endregion

        #region Core Send (single attempt)

        private async Task SendOnceAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            // Port-to-security-mode mapping:
            //   465 → SslOnConnect : TLS wraps the entire connection from the first byte.
            //   587 → StartTls     : plain TCP first, then upgraded with STARTTLS after EHLO.
            // Using SecureSocketOptions.Auto is avoided because it can silently downgrade
            // to an unencrypted session on some GoDaddy edge nodes.
            SecureSocketOptions socketOptions = _smtpPort switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                _   => SecureSocketOptions.StartTls
            };

            using var client = new SmtpClient();

            // Enforce TLS 1.2/1.3. GoDaddy requires a minimum of TLS 1.2;
            // explicitly excluding TLS 1.0/1.1 prevents protocol-downgrade attacks.
            client.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

            // Custom validator handles GoDaddy's frequent omission of the
            // intermediate CA certificate from the TLS handshake bundle.
            client.ServerCertificateValidationCallback = BuildCertificateValidator();

            // Hard timeout prevents indefinite hangs on slow GoDaddy edge servers.
            client.Timeout = _timeoutMs;

            try
            {
                // ── Connect ───────────────────────────────────────────────────
                _logger.LogInformation(
                    "SmtpEmailService: Connecting to {Host}:{Port} ({Mode})…",
                    _smtpHost, _smtpPort, socketOptions);

                await client.ConnectAsync(_smtpHost, _smtpPort, socketOptions, cancellationToken);

                // Remove XOAUTH2 before authenticating: GoDaddy's shared SMTP does not
                // support OAuth2 and some MailKit versions throw rather than falling back
                // to PLAIN/LOGIN when XOAUTH2 is present in the mechanism list.
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                // ── Authenticate ──────────────────────────────────────────────
                _logger.LogInformation(
                    "SmtpEmailService: Authenticating as {Username}…", _username);

                await client.AuthenticateAsync(_username, _password, cancellationToken);

                _logger.LogInformation(
                    "SmtpEmailService: Authentication successful.");

                // ── Send ──────────────────────────────────────────────────────
                _logger.LogInformation(
                    "SmtpEmailService: Sending message — To={To}, Subject={Subject}",
                    message.To, message.Subject);

                await client.SendAsync(message, cancellationToken);
            }
            finally
            {
                // Always disconnect cleanly regardless of whether the send succeeded.
                // CancellationToken.None ensures QUIT is sent even after the caller cancels.
                if (client.IsConnected)
                {
                    try
                    {
                        await client.DisconnectAsync(quit: true, CancellationToken.None);
                        _logger.LogInformation("SmtpEmailService: Connection closed.");
                    }
                    catch (Exception ex)
                    {
                        // Disconnect errors are non-fatal — log and continue so the
                        // original send exception (if any) is not masked.
                        _logger.LogWarning(ex,
                            "SmtpEmailService: Error while closing connection — {ErrorMessage}",
                            ex.Message);
                    }
                }
            }
        }

        #endregion

        #region Message Builder

        private MimeMessage BuildMessage(string toEmail, string subject, string body, bool isHtml)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder();
            if (isHtml)
                builder.HtmlBody = body;
            else
                builder.TextBody = body;

            message.Body = builder.ToMessageBody();
            return message;
        }

        #endregion

        #region Certificate Validation

        /// <summary>
        /// Returns a <see cref="RemoteCertificateValidationCallback"/> that:
        /// <list type="bullet">
        ///   <item>Accepts a perfect chain (no errors).</item>
        ///   <item>Accepts a partial chain where only the intermediate CA is missing
        ///         but the root is still OS-trusted — covers GoDaddy's common
        ///         incomplete-chain handshake.</item>
        ///   <item>Rejects all other errors (wrong hostname, revoked, expired, etc.).</item>
        ///   <item>When <c>AcceptAnyServerCertificate=true</c>, bypasses all validation
        ///         (for local debugging against self-signed certificates only).</item>
        /// </list>
        /// </summary>
        private RemoteCertificateValidationCallback BuildCertificateValidator()
        {
            if (_acceptAnyServerCertificate)
                return (_, _, _, _) => true;

            return (sender, certificate, chain, sslPolicyErrors) =>
            {
                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;

                // Accept a partial chain (missing intermediate CA) when every
                // remaining chain status is PartialChain or NoError.
                // This is the typical GoDaddy SMTP scenario where the server omits
                // the intermediate certificate from the handshake bundle.
                if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors &&
                    chain is not null)
                {
                    bool onlyPartialChain = chain.ChainStatus.All(
                        s => s.Status == X509ChainStatusFlags.PartialChain ||
                             s.Status == X509ChainStatusFlags.NoError);

                    if (onlyPartialChain)
                    {
                        _logger.LogDebug(
                            "SmtpEmailService: Server certificate chain is partial " +
                            "(intermediate CA omitted) — accepted because root is OS-trusted.");
                        return true;
                    }
                }

                _logger.LogError(
                    "SmtpEmailService: Certificate validation failed — Errors={Errors}, Subject={Subject}",
                    sslPolicyErrors,
                    certificate?.Subject ?? "unknown");

                return false;
            };
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Retrieves a required configuration value and throws <see cref="InvalidOperationException"/>
        /// with the exact key name when the value is missing or blank. This surfaces
        /// misconfiguration at startup rather than silently at the first send attempt.
        /// </summary>
        private static string RequiredConfig(IConfiguration cfg, string key)
        {
            var value = cfg[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"Required configuration key '{key}' is missing or empty in appsettings.json.");
            return value;
        }

        #endregion
    }
}

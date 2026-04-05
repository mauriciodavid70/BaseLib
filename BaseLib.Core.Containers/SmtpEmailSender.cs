using BaseLib.Core.Mail;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BaseLib.Core.Containers
{
    /// <summary>
    /// <see cref="IEmailSender"/> implementation that delivers messages via SMTP using MailKit.
    /// Supports optional username/password credentials.  Returns a failed <see cref="EmailResponse"/>
    /// instead of propagating SMTP exceptions, allowing callers to handle delivery failures gracefully.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly string host;
        private readonly int port;
        private readonly string? username;
        private readonly string? password;

        /// <summary>
        /// Initialises the sender with SMTP connection settings.
        /// </summary>
        /// <param name="host">Hostname or IP address of the SMTP server.</param>
        /// <param name="port">TCP port number of the SMTP server.</param>
        /// <param name="username">Optional SMTP login username.</param>
        /// <param name="password">Optional SMTP login password.</param>
        public SmtpEmailSender(string host, int port, string? username = null, string? password = null)
        {
            this.host = host;
            this.port = port;
            this.username = username;
            this.password = password;
        }

        /// <inheritdoc/>
        public async Task<EmailResponse> SendAsync(MimeMessage message)
        {
            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, SecureSocketOptions.Auto);

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    await client.AuthenticateAsync(username, password);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return new EmailResponse { Succeeded = true };
            }
            catch (SmtpCommandException)
            {
                return new EmailResponse { Succeeded = false };
            }
            catch (SmtpProtocolException)
            {
                return new EmailResponse { Succeeded = false };
            }
            catch (Exception)
            {
                return new EmailResponse { Succeeded = false };
            }
        }
    }
}

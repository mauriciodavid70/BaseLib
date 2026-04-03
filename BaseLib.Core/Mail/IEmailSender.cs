using BaseLib.Core.Models;
using MimeKit;

namespace BaseLib.Core.Mail;

/// <summary>
/// Sends email messages via an underlying mail transport.
/// The AWS implementation delegates to Amazon SES via <c>AmazonEmailSender</c>.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends the given <paramref name="message"/> using the underlying transport.
    /// </summary>
    /// <param name="message">The MIME message to send, constructed via <c>EmailMessageFactory</c>.</param>
    /// <returns>An <see cref="EmailResponse"/> indicating whether the send succeeded.</returns>
    Task<EmailResponse> SendAsync(MimeMessage message);
}

/// <summary>
/// Response returned by <see cref="IEmailSender.SendAsync"/>.
/// Inherits <see cref="CoreResponseBase.Succeeded"/> and <see cref="CoreResponseBase.ReasonCode"/>.
/// </summary>
public class EmailResponse : CoreResponseBase
{
}
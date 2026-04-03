using System.Net.Mime;
using MimeKit;
using MimeKit.Text;

namespace BaseLib.Core.Mail;

/// <summary>Factory for building <see cref="MimeMessage"/> instances ready to pass to <see cref="IEmailSender"/>.</summary>
public static class EmailMessageFactory
{
    /// <summary>
    /// Constructs a <see cref="MimeMessage"/> with the supplied recipients, subject, body, and optional
    /// CC, BCC, and file attachments.
    /// </summary>
    /// <param name="fromMail">Sender email address.</param>
    /// <param name="Tos">Required list of To recipients.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="content">Email body content.</param>
    /// <param name="format">Body format — defaults to HTML.</param>
    /// <param name="bccs">Optional BCC recipients.</param>
    /// <param name="ccs">Optional CC recipients.</param>
    /// <param name="attachments">Optional file attachments.</param>
    public static MimeMessage Create(
        string fromMail, IEnumerable<string> Tos, string subject, string content, TextFormat format = TextFormat.Html, IEnumerable<string>? bccs = null,
        IEnumerable<string>? ccs = null, IEnumerable<FileAttachment>? attachments = null)
    {
        var message = new MimeMessage()
        {
            Subject = subject
        };
        message.From.Add(new MailboxAddress(fromMail, fromMail));
        if (Tos == null || !Tos.Any())
        {
            throw new NullReferenceException("Los destinatarios no pueden estar vacíos");
        }
        message.To.AddRange(Tos.Select(x => new MailboxAddress(x, x)));
        message.Body = new TextPart(format) { Text = content };
        if (ccs != null && ccs.Any())
        {
            message.Cc.AddRange(ccs.Select(x => new MailboxAddress(x, x)));
        }
        if (bccs != null && bccs.Any())
        {
            message.Bcc.AddRange(bccs.Select(x => new MailboxAddress(x, x)));
        }
        if (attachments != null && attachments.Any())
        {
            var multipartMessage = new Multipart("mixed");
            foreach (var file in attachments)
            {
                var contentType = new System.Net.Mime.ContentType(MediaTypeNames.Application.Octet);
                try
                {
                    if(!string.IsNullOrEmpty(file.MediaType))
                    {
                        contentType = new System.Net.Mime.ContentType(file.MediaType);
                    }
                }
                catch(Exception)
                {
                    throw new InvalidDataException("MediaType inválido");
                }
                var attachment = new MimePart(contentType.MediaType)
                {
                    Content = new MimeContent(file.Stream),
                    ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = file.FileName
                };
                multipartMessage.Add(attachment);
            }
            multipartMessage.Add(message.Body);
            message.Body = multipartMessage;
        }
        return message;
    }
}

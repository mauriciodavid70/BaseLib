namespace BaseLib.Core.Mail;
/// <summary>Represents a file to attach to an email message built with <see cref="EmailMessageFactory"/>.</summary>
public class FileAttachment
{
    /// <summary>Stream containing the file contents.</summary>
    public Stream? Stream { get; set; }
    /// <summary>File name shown in the email client (e.g. <c>invoice.pdf</c>).</summary>
    public string? FileName { get; set; }
    /// <summary>MIME media type (e.g. <c>application/pdf</c>). Defaults to <c>application/octet-stream</c> if not set.</summary>
    public string? MediaType { get; set; }
}

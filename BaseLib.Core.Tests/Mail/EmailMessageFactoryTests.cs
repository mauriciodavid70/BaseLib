using BaseLib.Core.Mail;
using Xunit;

namespace BaseLib.Core.Tests.Mail;

/// <summary>Unit tests for <see cref="EmailMessageFactory"/>.</summary>
public class EmailMessageFactoryTests
{
    private const string FromMail = "sender@example.com";
    private const string ToMail = "recipient@example.com";
    private const string Subject = "Test Subject";
    private const string Body = "Test body";

    /// <summary>
    /// A <see cref="FileAttachment"/> whose <see cref="FileAttachment.Stream"/> is null
    /// must be silently skipped — no exception should be thrown and the message must still
    /// be constructed with the correct subject and recipient.
    /// </summary>
    [Fact]
    public void Create_WithNullStreamAttachment_SkipsAttachmentSilently()
    {
        // Arrange
        var nullStreamAttachment = new FileAttachment
        {
            Stream = null,
            FileName = "ghost.pdf",
            MediaType = "application/pdf"
        };

        // Act — must not throw
        var message = EmailMessageFactory.Create(
            FromMail,
            [ToMail],
            Subject,
            Body,
            attachments: [nullStreamAttachment]);

        // Assert: message is still valid with the correct metadata.
        Assert.Equal(Subject, message.Subject);
        Assert.Single(message.To);
    }

    /// <summary>
    /// A <see cref="FileAttachment"/> with a valid non-null <see cref="Stream"/> must be
    /// included in the returned message as a Multipart body.
    /// </summary>
    [Fact]
    public void Create_WithValidStreamAttachment_IncludesAttachment()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        var attachment = new FileAttachment
        {
            Stream = stream,
            FileName = "data.bin",
            MediaType = "application/octet-stream"
        };

        // Act
        var message = EmailMessageFactory.Create(
            FromMail,
            [ToMail],
            Subject,
            Body,
            attachments: [attachment]);

        // Assert: body is wrapped in a Multipart because at least one attachment was added.
        Assert.Contains("Multipart", message.Body?.GetType().Name ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// When the attachment list mixes null-stream and valid-stream entries, only the
    /// valid one should appear — the message body is a Multipart with exactly two children
    /// (one attachment MimePart and one body TextPart).
    /// </summary>
    [Fact]
    public void Create_WithMixedAttachments_IncludesOnlyNonNullStreams()
    {
        // Arrange
        using var stream = new MemoryStream([4, 5, 6]);
        var nullStreamAttachment = new FileAttachment { Stream = null, FileName = "skip.txt" };
        var validAttachment = new FileAttachment { Stream = stream, FileName = "keep.bin" };

        // Act
        var message = EmailMessageFactory.Create(
            FromMail,
            [ToMail],
            Subject,
            Body,
            attachments: [nullStreamAttachment, validAttachment]);

        // Assert: Multipart contains the body TextPart plus exactly one MimePart attachment.
        // multipart children: [attachment MimePart, body TextPart]
        var multipart = Assert.IsAssignableFrom<MimeKit.Multipart>(message.Body);
        Assert.Equal(2, multipart.Count);
    }

    [Fact]
    public void Create_WithReplyTo_PopulatesMimeMessageReplyTo()
    {
        // Arrange
        var replyToAddresses = new[] { "replyto@example.com", "other@example.com" };

        // Act
        var message = EmailMessageFactory.Create(
            fromMail: "sender@example.com",
            Tos: new[] { "recipient@example.com" },
            subject: "Test Subject",
            content: "Test Body",
            replyTo: replyToAddresses);

        // Assert
        Assert.Equal(2, message.ReplyTo.Count);
        var addresses = message.ReplyTo.Mailboxes.Select(m => m.Address).ToList();
        Assert.Contains("replyto@example.com", addresses);
        Assert.Contains("other@example.com", addresses);
    }

    [Fact]
    public void Create_WithoutReplyTo_LeavesReplyToEmpty()
    {
        // Act
        var message = EmailMessageFactory.Create(
            fromMail: "sender@example.com",
            Tos: new[] { "recipient@example.com" },
            subject: "Test Subject",
            content: "Test Body");

        // Assert
        Assert.Empty(message.ReplyTo);
    }
}

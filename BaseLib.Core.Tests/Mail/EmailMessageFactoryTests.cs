using BaseLib.Core.Mail;
using Xunit;

namespace BaseLib.Core.Tests.Mail;

public class EmailMessageFactoryTests
{
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

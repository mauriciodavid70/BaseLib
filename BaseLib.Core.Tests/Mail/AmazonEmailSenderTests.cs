using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using BaseLib.Core.AmazonCloud.Mail;
using MimeKit;
using Moq;
using Xunit;

namespace BaseLib.Core.Tests.Mail;

public class AmazonEmailSenderTests
{
    private static MimeMessage BuildMessage(IEnumerable<string>? replyToAddresses = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("sender@example.com", "sender@example.com"));
        message.To.Add(new MailboxAddress("recipient@example.com", "recipient@example.com"));
        message.Subject = "Test";
        message.Body = new MimeKit.TextPart("plain") { Text = "Hello" };

        if (replyToAddresses != null)
        {
            foreach (var address in replyToAddresses)
            {
                message.ReplyTo.Add(new MailboxAddress(address, address));
            }
        }

        return message;
    }

    [Fact]
    public async Task SendAsync_WithReplyTo_SetsReplyToAddressesOnRequest()
    {
        // Arrange
        SendEmailRequest? capturedRequest = null;
        var sesMock = new Mock<IAmazonSimpleEmailServiceV2>();
        sesMock
            .Setup(s => s.SendEmailAsync(It.IsAny<SendEmailRequest>(), default))
            .Callback<SendEmailRequest, System.Threading.CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new SendEmailResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });

        var sender = new AmazonEmailSender(sesMock.Object);
        var message = BuildMessage(replyToAddresses: new[] { "replyto@example.com" });

        // Act
        await sender.SendAsync(message);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.ReplyToAddresses);
        Assert.Single(capturedRequest.ReplyToAddresses);
        Assert.Equal("replyto@example.com", capturedRequest.ReplyToAddresses[0]);
    }

    [Fact]
    public async Task SendAsync_WithoutReplyTo_ReplyToAddressesIsNullOrEmpty()
    {
        // Arrange
        SendEmailRequest? capturedRequest = null;
        var sesMock = new Mock<IAmazonSimpleEmailServiceV2>();
        sesMock
            .Setup(s => s.SendEmailAsync(It.IsAny<SendEmailRequest>(), default))
            .Callback<SendEmailRequest, System.Threading.CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new SendEmailResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });

        var sender = new AmazonEmailSender(sesMock.Object);
        var message = BuildMessage(); // no ReplyTo

        // Act
        await sender.SendAsync(message);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(
            capturedRequest.ReplyToAddresses == null || capturedRequest.ReplyToAddresses.Count == 0,
            "ReplyToAddresses should be null or empty when no Reply-To is set");
    }
}

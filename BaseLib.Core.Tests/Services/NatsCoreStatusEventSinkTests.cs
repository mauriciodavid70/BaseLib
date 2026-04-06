using System.Text;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using BaseLib.Core.Services;
using BaseLib.Core.Services.Nats;
using Moq;
using NATS.Client.Core;
using Xunit;

namespace BaseLib.Core.Tests.Services
{
    public class NatsCoreStatusEventSinkTests
    {
        private class TestResponse : CoreResponseBase { }

        private readonly Mock<INatsConnection> natsMock;
        private readonly NatsCoreStatusEventSink sink;

        private readonly List<string> publishedSubjects = new();

        public NatsCoreStatusEventSinkTests()
        {
            CoreSerializer.Initialize(new CoreJsonSerializer());

            natsMock = new Mock<INatsConnection>();
            // Signature: (subject, data, headers?, replyTo?, serializer?, opts?, ct)
            natsMock
                .Setup(n => n.PublishAsync<byte[]>(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<NatsHeaders?>(),
                    It.IsAny<string?>(),
                    It.IsAny<INatsSerialize<byte[]>?>(),
                    It.IsAny<NatsPubOpts?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, byte[], NatsHeaders?, string?, INatsSerialize<byte[]>?, NatsPubOpts?, CancellationToken>(
                    (subject, data, headers, replyTo, serializer, opts, ct) =>
                        publishedSubjects.Add(subject))
                .Returns(ValueTask.CompletedTask);

            sink = new NatsCoreStatusEventSink(natsMock.Object);
        }

        [Fact]
        public async Task WriteAsync_SucceededEvent_UsesSucceededSubject()
        {
            var statusEvent = new CoreStatusEvent
            {
                ModuleName = "orders",
                ServiceName = "OrderService",
                Response = new TestResponse { Succeeded = true }
            };

            await sink.WriteAsync(statusEvent);

            Assert.Single(publishedSubjects);
            Assert.Equal("orders.OrderService.succeeded", publishedSubjects[0]);
        }

        [Fact]
        public async Task WriteAsync_FailedEvent_UsesFailedSubject()
        {
            var statusEvent = new CoreStatusEvent
            {
                ModuleName = "orders",
                ServiceName = "OrderService",
                Response = new TestResponse { Succeeded = false }
            };

            await sink.WriteAsync(statusEvent);

            Assert.Single(publishedSubjects);
            Assert.Equal("orders.OrderService.failed", publishedSubjects[0]);
        }

        [Fact]
        public async Task WriteAsync_NullResponse_UsesFailedSubject()
        {
            var statusEvent = new CoreStatusEvent
            {
                ModuleName = "payments",
                ServiceName = "PaymentService",
                Response = null
            };

            await sink.WriteAsync(statusEvent);

            Assert.Single(publishedSubjects);
            Assert.Equal("payments.PaymentService.failed", publishedSubjects[0]);
        }

        [Fact]
        public async Task WriteAsync_SubjectContainsModuleAndServiceName()
        {
            var statusEvent = new CoreStatusEvent
            {
                ModuleName = "inventory",
                ServiceName = "ReserveStock",
                Response = new TestResponse { Succeeded = true }
            };

            await sink.WriteAsync(statusEvent);

            Assert.Single(publishedSubjects);
            Assert.Contains("inventory", publishedSubjects[0]);
            Assert.Contains("ReserveStock", publishedSubjects[0]);
        }
    }
}

using System.Text;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using BaseLib.Core.Services;
using BaseLib.Core.Services.RabbitMQ;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace BaseLib.Core.Tests.Services
{
    public class RabbitMqCoreStatusEventSinkTests
    {
        private class TestResponse : CoreResponseBase { }

        private readonly Mock<IConnection> connectionMock;
        private readonly Mock<IChannel> channelMock;
        private readonly RabbitMqOptions options;
        private readonly RabbitMqCoreStatusEventSink sink;

        private readonly List<(string Exchange, string RoutingKey)> published = new();

        public RabbitMqCoreStatusEventSinkTests()
        {
            CoreSerializer.Initialize(new CoreJsonSerializer());

            options = new RabbitMqOptions
            {
                ExchangeName = "test.services",
                EventExchangeName = "test.events"
            };

            channelMock = new Mock<IChannel>();

            channelMock
                .Setup(c => c.ExchangeDeclareAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>>(),
                    It.IsAny<bool>(), It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            channelMock
                .Setup(c => c.BasicPublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<BasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
                    (exchange, routingKey, mandatory, props, body, ct) =>
                        published.Add((exchange, routingKey)))
                .Returns(new ValueTask());

            channelMock.Setup(c => c.DisposeAsync()).Returns(new ValueTask());

            connectionMock = new Mock<IConnection>();
            connectionMock
                .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(channelMock.Object);

            sink = new RabbitMqCoreStatusEventSink(connectionMock.Object, options);
        }

        [Fact]
        public async Task WriteAsync_SucceededEvent_UsesSucceededRoutingKey()
        {
            var statusEvent = new CoreStatusEvent
            {
                ServiceName = "OrderService",
                ModuleName = "orders",
                Response = new TestResponse { Succeeded = true }
            };

            await sink.WriteAsync(statusEvent);

            Assert.Single(published);
            Assert.Equal("OrderService.succeeded", published[0].RoutingKey);
        }

        [Fact]
        public async Task WriteAsync_FailedEvent_UsesFailedRoutingKey()
        {
            var statusEvent = new CoreStatusEvent
            {
                ServiceName = "OrderService",
                ModuleName = "orders",
                Response = new TestResponse { Succeeded = false }
            };

            await sink.WriteAsync(statusEvent);

            Assert.Single(published);
            Assert.Equal("OrderService.failed", published[0].RoutingKey);
        }

        [Fact]
        public async Task WriteAsync_NullResponse_UsesFailedRoutingKey()
        {
            var statusEvent = new CoreStatusEvent
            {
                ServiceName = "PaymentService",
                ModuleName = "payments",
                Response = null
            };

            await sink.WriteAsync(statusEvent);

            Assert.Single(published);
            Assert.Equal("PaymentService.failed", published[0].RoutingKey);
        }

        [Fact]
        public async Task WriteAsync_PublishesToEventExchange()
        {
            var statusEvent = new CoreStatusEvent
            {
                ServiceName = "OrderService",
                Response = new TestResponse { Succeeded = true }
            };

            await sink.WriteAsync(statusEvent);

            Assert.Single(published);
            Assert.Equal(options.EventExchangeName, published[0].Exchange);
        }
    }
}

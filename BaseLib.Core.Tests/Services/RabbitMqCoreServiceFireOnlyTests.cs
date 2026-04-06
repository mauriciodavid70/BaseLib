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
    public class RabbitMqCoreServiceFireOnlyTests
    {
        private class TestRequest : CoreRequestBase { }

        private class TestServiceResponse : CoreResponseBase { }

        private class TestService : CoreServiceBase<TestRequest, TestServiceResponse>
        {
            protected override Task<TestServiceResponse> RunAsync()
                => Task.FromResult(new TestServiceResponse { Succeeded = true });
        }

        private class TestLongRunning : ICoreLongRunningService
        {
            public Task<CoreResponseBase> ResumeAsync(string operationId)
                => Task.FromResult<CoreResponseBase>(new TestServiceResponse { Succeeded = true });
        }

        private readonly Mock<IConnection> connectionMock;
        private readonly Mock<IChannel> channelMock;
        private readonly RabbitMqOptions options;
        private readonly RabbitMqCoreServiceFireOnly fireOnly;

        private readonly List<(string Exchange, string RoutingKey, string Body)> published = new();

        public RabbitMqCoreServiceFireOnlyTests()
        {
            CoreSerializer.Initialize(new CoreJsonSerializer());

            options = new RabbitMqOptions
            {
                ExchangeName = "test.services",
                EventExchangeName = "test.events"
            };

            channelMock = new Mock<IChannel>();

            // ExchangeDeclareAsync → Task
            channelMock
                .Setup(c => c.ExchangeDeclareAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>>(),
                    It.IsAny<bool>(), It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, body, ct) → ValueTask (non-extension)
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
                        published.Add((exchange, routingKey, Encoding.UTF8.GetString(body.Span))))
                .Returns(new ValueTask());

            channelMock.Setup(c => c.DisposeAsync()).Returns(new ValueTask());

            connectionMock = new Mock<IConnection>();
            connectionMock
                .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(channelMock.Object);

            fireOnly = new RabbitMqCoreServiceFireOnly(connectionMock.Object, options);
        }

        [Fact]
        public async Task FireAsync_Generic_UsesAssemblyQualifiedTypeNameAsRoutingKey()
        {
            await fireOnly.FireAsync<TestService>(new TestRequest());

            var type = typeof(TestService);
            var expectedKey = $"{type.FullName}, {type.Assembly.GetName().Name}";
            Assert.Single(published);
            Assert.Equal(expectedKey, published[0].RoutingKey);
        }

        [Fact]
        public async Task FireAsync_Generic_PublishesToConfiguredExchange()
        {
            await fireOnly.FireAsync<TestService>(new TestRequest());

            Assert.Single(published);
            Assert.Equal(options.ExchangeName, published[0].Exchange);
        }

        [Fact]
        public async Task FireAsync_Generic_SerializesRunAsyncMessage()
        {
            await fireOnly.FireAsync<TestService>(new TestRequest(), correlationId: "corr-1");

            Assert.Single(published);
            var msg = CoreSerializer.Deserialize<FireAsyncMessage>(published[0].Body);
            Assert.NotNull(msg);
            Assert.Equal("RunAsync", msg!.Method);
            Assert.Equal("corr-1", msg.CorrelationId);
        }

        [Fact]
        public async Task ResumeAsync_Generic_PublishesResumeAsyncMessage()
        {
            await fireOnly.ResumeAsync<TestLongRunning>("op-123", correlationId: "corr-2");

            Assert.Single(published);
            var msg = CoreSerializer.Deserialize<FireAsyncMessage>(published[0].Body);
            Assert.NotNull(msg);
            Assert.Equal("ResumeAsync", msg!.Method);
            Assert.Equal("op-123", msg.OperationId);
            Assert.Equal("corr-2", msg.CorrelationId);
        }

        [Fact]
        public async Task ResumeAsync_Generic_UsesCorrectRoutingKey()
        {
            await fireOnly.ResumeAsync<TestLongRunning>("op-123");

            var type = typeof(TestLongRunning);
            var expectedKey = $"{type.FullName}, {type.Assembly.GetName().Name}";
            Assert.Single(published);
            Assert.Equal(expectedKey, published[0].RoutingKey);
        }

        [Fact]
        public async Task FireManyAsync_UsesSingleChannelForBatch()
        {
            var requests = new CoreRequestBase[]
            {
                new TestRequest(),
                new TestRequest(),
                new TestRequest()
            };

            await fireOnly.FireManyAsync<TestService>(requests);

            // 3 messages published
            Assert.Equal(3, published.Count);

            // CreateChannelAsync: once for exchange init, once for the batch (single channel)
            connectionMock.Verify(c => c.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task FireManyAsync_EachMessageHasRunAsyncMethod()
        {
            var requests = new CoreRequestBase[] { new TestRequest(), new TestRequest() };

            await fireOnly.FireManyAsync<TestService>(requests);

            Assert.Equal(2, published.Count);
            foreach (var (_, _, body) in published)
            {
                var msg = CoreSerializer.Deserialize<FireAsyncMessage>(body);
                Assert.Equal("RunAsync", msg!.Method);
            }
        }
    }
}

using System.Text;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using BaseLib.Core.Services;
using BaseLib.Core.Services.Nats;
using Moq;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Xunit;

namespace BaseLib.Core.Tests.Services
{
    public class NatsCoreServiceFireOnlyTests
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

        private readonly Mock<INatsJSContext> jsMock;
        private readonly NatsTransportOptions options;
        private readonly NatsCoreServiceFireOnly fireOnly;

        private readonly List<(string Subject, string Body)> published = new();

        public NatsCoreServiceFireOnlyTests()
        {
            CoreSerializer.Initialize(new CoreJsonSerializer());

            options = new NatsTransportOptions
            {
                StreamName = "test-stream",
                DispatchSubject = "test.dispatch"
            };

            jsMock = new Mock<INatsJSContext>();
            jsMock
                .Setup(j => j.PublishAsync<byte[]>(
                    It.IsAny<string>(),
                    It.IsAny<byte[]?>(),
                    It.IsAny<INatsSerialize<byte[]>?>(),
                    It.IsAny<NatsJSPubOpts?>(),
                    It.IsAny<NatsHeaders?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, byte[]?, INatsSerialize<byte[]>?, NatsJSPubOpts?, NatsHeaders?, CancellationToken>(
                    (subject, data, serializer, opts, headers, ct) =>
                        published.Add((subject, Encoding.UTF8.GetString(data ?? Array.Empty<byte>()))))
                .ReturnsAsync(new PubAckResponse());

            fireOnly = new NatsCoreServiceFireOnly(jsMock.Object, options);
        }

        [Fact]
        public async Task FireAsync_Generic_PublishesToDispatchSubject()
        {
            await fireOnly.FireAsync<TestService>(new TestRequest());

            Assert.Single(published);
            Assert.Equal(options.DispatchSubject, published[0].Subject);
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
        public async Task FireAsync_Generic_IncludesTypeName()
        {
            await fireOnly.FireAsync<TestService>(new TestRequest());

            Assert.Single(published);
            var msg = CoreSerializer.Deserialize<FireAsyncMessage>(published[0].Body);
            Assert.NotNull(msg);
            var type = typeof(TestService);
            var expectedTypeName = $"{type.FullName}, {type.Assembly.GetName().Name}";
            Assert.Equal(expectedTypeName, msg!.TypeName);
        }

        [Fact]
        public async Task ResumeAsync_Generic_PublishesResumeAsyncMessage()
        {
            await fireOnly.ResumeAsync<TestLongRunning>("op-abc", correlationId: "corr-2");

            Assert.Single(published);
            var msg = CoreSerializer.Deserialize<FireAsyncMessage>(published[0].Body);
            Assert.NotNull(msg);
            Assert.Equal("ResumeAsync", msg!.Method);
            Assert.Equal("op-abc", msg.OperationId);
            Assert.Equal("corr-2", msg.CorrelationId);
        }

        [Fact]
        public async Task ResumeAsync_Generic_PublishesToDispatchSubject()
        {
            await fireOnly.ResumeAsync<TestLongRunning>("op-abc");

            Assert.Single(published);
            Assert.Equal(options.DispatchSubject, published[0].Subject);
        }
    }
}

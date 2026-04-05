using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using BaseLib.Core.Services;
using Moq;
using Xunit;

namespace BaseLib.Core.Tests.Services
{
    public class CoreMessageDispatcherTests
    {
        // Simple concrete request type for testing
        private class TestRequest : CoreRequestBase { }

        // Minimal ICoreMessageEnvelope implementation for tests
        private sealed record TestEnvelope(string Body, string MessageId) : ICoreMessageEnvelope;

        private readonly Mock<ICoreServiceRunner> runnerMock;
        private readonly CoreMessageDispatcher dispatcher;

        public CoreMessageDispatcherTests()
        {
            // Initialize CoreSerializer with the standard JSON serializer
            CoreSerializer.Initialize(new CoreJsonSerializer());

            runnerMock = new Mock<ICoreServiceRunner>();
            runnerMock
                .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CoreRequestBase>(), It.IsAny<string?>(), It.IsAny<bool>()))
                .ReturnsAsync(new TestResponse { Succeeded = true });
            runnerMock
                .Setup(r => r.ResumeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new TestResponse { Succeeded = true });

            dispatcher = new CoreMessageDispatcher(runnerMock.Object);
        }

        private class TestResponse : CoreResponseBase { }

        [Fact]
        public async Task DispatchAsync_RunAsyncMethod_CallsRunnerRunAsync()
        {
            // Arrange
            var request = new TestRequest();
            var typeName = typeof(TestRequest).AssemblyQualifiedName!;
            var payload = new
            {
                TypeName = typeName,
                Method = "RunAsync",
                Request = (object)request,
                CorrelationId = "corr-1",
                IsLongRunningChild = false
            };
            var body = CoreSerializer.Serialize(payload);
            var envelope = new TestEnvelope(body, "msg-1");

            // Act
            await dispatcher.DispatchAsync(envelope);

            // Assert
            runnerMock.Verify(r => r.RunAsync(typeName, It.IsAny<CoreRequestBase>(), "corr-1", false), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_AbsentMethod_DefaultsToRunAsync()
        {
            // Arrange — no Method field
            var request = new TestRequest();
            var typeName = typeof(TestRequest).AssemblyQualifiedName!;
            var payload = new
            {
                TypeName = typeName,
                Request = (object)request,
                CorrelationId = (string?)null,
                IsLongRunningChild = false
            };
            var body = CoreSerializer.Serialize(payload);
            var envelope = new TestEnvelope(body, "msg-2");

            // Act
            await dispatcher.DispatchAsync(envelope);

            // Assert
            runnerMock.Verify(r => r.RunAsync(typeName, It.IsAny<CoreRequestBase>(), null, false), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_ResumeAsyncMethod_CallsRunnerResumeAsync()
        {
            // Arrange
            var typeName = typeof(TestRequest).AssemblyQualifiedName!;
            var operationId = "op-abc";
            var payload = new
            {
                TypeName = typeName,
                Method = "ResumeAsync",
                OperationId = operationId
            };
            var body = CoreSerializer.Serialize(payload);
            var envelope = new TestEnvelope(body, "msg-3");

            // Act
            await dispatcher.DispatchAsync(envelope);

            // Assert
            runnerMock.Verify(r => r.ResumeAsync(typeName, operationId), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_UnknownMethod_ThrowsNotSupportedException()
        {
            // Arrange
            var typeName = typeof(TestRequest).AssemblyQualifiedName!;
            var payload = new
            {
                TypeName = typeName,
                Method = "DeleteAsync"
            };
            var body = CoreSerializer.Serialize(payload);
            var envelope = new TestEnvelope(body, "msg-4");

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => dispatcher.DispatchAsync(envelope));
        }
    }
}

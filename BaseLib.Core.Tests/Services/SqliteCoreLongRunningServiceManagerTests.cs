using BaseLib.Core.Models;
using BaseLib.Core.Services;
using BaseLib.Core.Sqlite;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

namespace BaseLib.Core.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="SqliteCoreLongRunningServiceManager"/> using an in-memory SQLite database.
    /// </summary>
    public class SqliteCoreLongRunningServiceManagerTests : IDisposable
    {
        private readonly SqliteConnection sharedConnection;
        private readonly Mock<ICoreServiceFireOnly> invokerMock;
        private readonly SqliteCoreLongRunningServiceManager manager;
        private readonly string connectionString;

        public SqliteCoreLongRunningServiceManagerTests()
        {
            // Use a named shared-cache in-memory database. The sharedConnection keeps the
            // database alive for the lifetime of the test; each factory call opens a new
            // connection to the same in-memory database via the shared-cache URI.
            var dbName = Guid.NewGuid().ToString("N");
            connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";

            sharedConnection = new SqliteConnection(connectionString);
            sharedConnection.Open();

            invokerMock = new Mock<ICoreServiceFireOnly>();

            manager = new SqliteCoreLongRunningServiceManager(
                () => { var c = new SqliteConnection(connectionString); c.Open(); return c; },
                invokerMock.Object);
        }

        public void Dispose()
        {
            sharedConnection.Dispose();
        }

        // Helper to build a parent-suspended event
        private static CoreStatusEvent MakeParentSuspendedEvent(string operationId, string correlationId, int childrenCount) =>
            new CoreStatusEvent
            {
                OperationId = operationId,
                CorrelationId = correlationId,
                TypeName = "TestService",
                Status = CoreServiceStatus.Suspended,
                StartedOn = DateTimeOffset.UtcNow,
                FinishedOn = DateTimeOffset.UtcNow,
                ChildrenCount = childrenCount
            };

        // Helper to build a child-finished event
        private static CoreStatusEvent MakeChildFinishedEvent(string operationId, string correlationId, bool succeeded) =>
            new CoreStatusEvent
            {
                OperationId = operationId,
                CorrelationId = correlationId,
                TypeName = "ChildService",
                Status = CoreServiceStatus.Finished,
                StartedOn = DateTimeOffset.UtcNow,
                FinishedOn = DateTimeOffset.UtcNow,
                Response = new TestResponse { Succeeded = succeeded, ReasonCode = succeeded ? CoreReasonCode.Succeeded : CoreReasonCode.Failed }
            };

        // Helper to build a parent-finished event
        private static CoreStatusEvent MakeParentFinishedEvent(string operationId, bool succeeded) =>
            new CoreStatusEvent
            {
                OperationId = operationId,
                CorrelationId = operationId,
                TypeName = "TestService",
                Status = CoreServiceStatus.Finished,
                StartedOn = DateTimeOffset.UtcNow,
                FinishedOn = DateTimeOffset.UtcNow,
                Response = new TestResponse { Succeeded = succeeded, ReasonCode = succeeded ? CoreReasonCode.Succeeded : CoreReasonCode.Failed }
            };

        [Fact]
        public async Task HandleParentSuspendedAsync_ChildrenNotYetDone_DoesNotCallResume()
        {
            // Arrange
            var parentOp = Guid.NewGuid().ToString();
            var corrId = Guid.NewGuid().ToString();
            var evt = MakeParentSuspendedEvent(parentOp, corrId, childrenCount: 3);

            // Act — no children have finished yet
            await manager.HandleParentSuspendedAsync(evt);

            // Assert — ResumeAsync should NOT be called because 0 completed < 3 required
            invokerMock.Verify(x => x.ResumeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleParentSuspendedAsync_AllChildrenAlreadyDone_CallsResume()
        {
            // Arrange — insert children first so they exist before the parent suspends
            var parentOp = Guid.NewGuid().ToString();
            var corrId = parentOp; // children link to parent via correlation id = parent operation id

            var childEvents = new[]
            {
                MakeChildFinishedEvent(Guid.NewGuid().ToString(), corrId, succeeded: true),
                MakeChildFinishedEvent(Guid.NewGuid().ToString(), corrId, succeeded: true)
            };
            await manager.HandleChildrenFinishedAsync(childEvents);

            // Clear any invocations from child-finishing before parent suspends
            invokerMock.Invocations.Clear();

            // Parent suspends with exactly 2 children expected
            var evt = MakeParentSuspendedEvent(parentOp, corrId, childrenCount: 2);

            // Act
            await manager.HandleParentSuspendedAsync(evt);

            // Assert — all children already done, so resume should be triggered
            invokerMock.Verify(
                x => x.ResumeAsync("TestService", parentOp, corrId),
                Times.Once);
        }

        [Fact]
        public async Task HandleChildrenFinishedAsync_CountNotMet_DoesNotCallResume()
        {
            // Arrange — parent needs 3 children but only 2 finish
            var parentOp = Guid.NewGuid().ToString();
            var corrId = parentOp;

            await manager.HandleParentSuspendedAsync(MakeParentSuspendedEvent(parentOp, corrId, childrenCount: 3));
            invokerMock.Invocations.Clear();

            var twoChildren = new[]
            {
                MakeChildFinishedEvent(Guid.NewGuid().ToString(), corrId, succeeded: true),
                MakeChildFinishedEvent(Guid.NewGuid().ToString(), corrId, succeeded: true)
            };

            // Act
            await manager.HandleChildrenFinishedAsync(twoChildren);

            // Assert — only 2 of 3 done; no resume
            invokerMock.Verify(x => x.ResumeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleChildrenFinishedAsync_CountMet_CallsResume()
        {
            // Arrange — parent needs 2 children, both finish
            var parentOp = Guid.NewGuid().ToString();
            var corrId = parentOp;

            await manager.HandleParentSuspendedAsync(MakeParentSuspendedEvent(parentOp, corrId, childrenCount: 2));
            invokerMock.Invocations.Clear();

            var twoChildren = new[]
            {
                MakeChildFinishedEvent(Guid.NewGuid().ToString(), corrId, succeeded: true),
                MakeChildFinishedEvent(Guid.NewGuid().ToString(), corrId, succeeded: false)
            };

            // Act
            await manager.HandleChildrenFinishedAsync(twoChildren);

            // Assert — 2 of 2 done; resume must be called
            invokerMock.Verify(
                x => x.ResumeAsync("TestService", parentOp, corrId),
                Times.Once);
        }

        [Fact]
        public async Task HandleParentFinishedAsync_UpdatesRowInDatabase()
        {
            // Arrange — insert the parent row first
            var parentOp = Guid.NewGuid().ToString();
            var corrId = parentOp;
            await manager.HandleParentSuspendedAsync(MakeParentSuspendedEvent(parentOp, corrId, childrenCount: 0));

            var finishedEvt = MakeParentFinishedEvent(parentOp, succeeded: true);

            // Act
            await manager.HandleParentFinishedAsync(finishedEvt);

            // Assert — verify the row was updated (SERVICE_STATUS = Finished)
            using var verifyConn = new SqliteConnection(connectionString);
            verifyConn.Open();
            using var cmd = new SqliteCommand(
                "SELECT SERVICE_STATUS FROM LONG_RUNNING_BATCH WHERE OPERATION_ID = @id",
                verifyConn);
            cmd.Parameters.AddWithValue("@id", parentOp);
            var status = (long)(await cmd.ExecuteScalarAsync())!;

            Assert.Equal((int)CoreServiceStatus.Finished, (int)status);
        }

        // Minimal concrete response for test helpers
        private class TestResponse : CoreResponseBase { }
    }
}

using BaseLib.Core.Containers;
using BaseLib.Core.Serialization;
using Xunit;

namespace BaseLib.Core.Tests.Services
{
    public class FileSystemCoreServiceStateStoreTests : IDisposable
    {
        private readonly string tempDir;
        private readonly FileSystemCoreServiceStateStore store;

        public FileSystemCoreServiceStateStoreTests()
        {
            // Initialize serializer for FileSystemCoreServiceStateStore (uses CoreSerializer internally)
            CoreSerializer.Initialize(new CoreJsonSerializer());

            tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            store = new FileSystemCoreServiceStateStore(tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }

        [Fact]
        public async Task WriteAsync_ThenReadAsync_ReturnsOriginalState()
        {
            // Arrange
            var operationId = "op-roundtrip";
            var state = new Dictionary<string, object?>
            {
                ["key1"] = "value1",
                ["key2"] = 42
            };

            // Act
            await store.WriteAsync(operationId, state);
            var result = await store.ReadAsync(operationId);

            // Assert
            Assert.Equal("value1", result["key1"]?.ToString());
            Assert.NotNull(result["key2"]);
        }

        [Fact]
        public async Task ReadAsync_MissingOperationId_ThrowsInvalidOperationException()
        {
            // Arrange
            var operationId = "op-does-not-exist";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.ReadAsync(operationId));
        }

        [Fact]
        public async Task WriteAsync_CreatesFileUnderRootDirectory()
        {
            // Arrange
            var operationId = "op-file-check";
            var state = new Dictionary<string, object?> { ["x"] = "y" };

            // Act
            await store.WriteAsync(operationId, state);

            // Assert
            Assert.True(File.Exists(Path.Combine(tempDir, $"{operationId}.json")));
        }
    }
}

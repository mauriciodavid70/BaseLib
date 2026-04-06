using BaseLib.Core.Serialization;
using BaseLib.Core.Services;

namespace BaseLib.Core.Local
{
    /// <summary>
    /// <see cref="ICoreServiceStateStore"/> implementation that persists long-running service
    /// state as JSON files on the local file system.  The <c>operationId</c> is used as the
    /// file name (with a <c>.json</c> extension) under a configurable root directory.
    /// Intended for single-instance local/dev use; not safe for multi-replica deployments.
    /// </summary>
    public class FileSystemCoreServiceStateStore : ICoreServiceStateStore
    {
        private readonly string rootDirectory;

        /// <summary>
        /// Initialises the store with the directory under which state files are written.
        /// </summary>
        /// <param name="rootDirectory">Absolute or relative path to the directory that will contain state files.</param>
        public FileSystemCoreServiceStateStore(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
            Directory.CreateDirectory(rootDirectory);
        }

        /// <inheritdoc/>
        public async Task WriteAsync(string operationId, IDictionary<string, object?> state)
        {
            var path = GetFilePath(operationId);
            var json = CoreSerializer.Serialize(state);
            await File.WriteAllTextAsync(path, json);
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, object?>> ReadAsync(string operationId)
        {
            var path = GetFilePath(operationId);
            if (!File.Exists(path))
                throw new InvalidOperationException($"State not found for operationId '{operationId}'. Expected file: {path}");

            var json = await File.ReadAllTextAsync(path);
            return CoreSerializer.Deserialize<IDictionary<string, object?>>(json)
                ?? throw new InvalidOperationException($"State file for operationId '{operationId}' deserialized to null.");
        }

        private string GetFilePath(string operationId) =>
            Path.Combine(rootDirectory, $"{operationId}.json");
    }
}

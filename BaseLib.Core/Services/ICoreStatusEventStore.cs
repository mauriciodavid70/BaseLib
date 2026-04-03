using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Persists and retrieves <see cref="CoreStatusEvent"/> records for auditing and
    /// long-running service coordination. Used by <see cref="ICoreLongRunningServiceManager"/>
    /// implementations to track parent/child relationships.
    /// </summary>
    public interface ICoreStatusEventStore
    {
        /// <summary>Persists the given <paramref name="statusEvent"/>.</summary>
        /// <returns>The number of records written.</returns>
        Task<int> WriteAsync(CoreStatusEvent statusEvent);

        /// <summary>
        /// Reads the most recent <see cref="CoreStatusEvent"/> for the given <paramref name="correlationId"/>.
        /// </summary>
        /// <param name="correlationId">The correlation identifier to look up.</param>
        Task<CoreStatusEvent> ReadAsync(string correlationId);
    }
}
using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Converts a <see cref="CoreStatusEvent"/> into a <see cref="Models.JournalEntry"/> and
    /// persists it via <see cref="IJournalEntryWriter"/>. Called by event processors after
    /// each service execution to maintain an audit trail.
    /// </summary>
    public interface IJournalEventHandler
    {
        /// <summary>
        /// Processes the <paramref name="statusEvent"/> and writes a journal entry.
        /// </summary>
        /// <param name="statusEvent">The status event to journal.</param>
        /// <returns>The number of records written.</returns>
        Task<int> HandleAsync(CoreStatusEvent statusEvent);
    }
}
using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Persists a <see cref="JournalEntry"/> to a durable store after each service execution.
    /// The default implementation targets MySQL via <c>BaseLib.Core.MySql</c>.
    /// </summary>
    /// <remarks>
    /// Best practice: store only the journal entry (metadata) in a relational database.
    /// Request and response payloads should be stored separately in a secure object store.
    /// </remarks>
    public interface IJournalEntryWriter
    {
        /// <summary>
        /// Writes the <paramref name="entry"/> to the underlying store.
        /// </summary>
        /// <param name="entry">The journal entry to persist.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> WriteAsync(JournalEntry entry);
    }
}
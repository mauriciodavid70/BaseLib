using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// No-op implementation of <see cref="ICoreStatusEventSink"/> used as the default when no
    /// event sink is injected. Silently discards all events.
    /// </summary>
    public class NullCoreEventSink : ICoreStatusEventSink
    {
        /// <inheritdoc/>
        public Task WriteAsync(CoreStatusEvent statusEvent)
        {
             return Task.CompletedTask;
        }
    }
}


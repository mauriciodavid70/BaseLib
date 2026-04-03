using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Publishes service status events to an external messaging system for
    /// event-driven choreography between services.
    /// Implement this interface to target a specific transport (e.g. SNS, Azure Service Bus).
    /// Use <see cref="NullCoreEventSink"/> when no event publishing is needed.
    /// </summary>
    public interface ICoreStatusEventSink
    {
        /// <summary>
        /// Publishes a <see cref="CoreStatusEvent"/> to the underlying transport.
        /// </summary>
        /// <param name="statusEvent">The event describing the current service status.</param>
        Task WriteAsync(CoreStatusEvent statusEvent);
    }
}
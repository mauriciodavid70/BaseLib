
using System;
using BaseLib.Core.Models;

namespace BaseLib.Core.Services;

/// <summary>
/// Coordinates the lifecycle of a long-running service by reacting to status events
/// emitted by the parent and its children.
/// Implement this interface in the infrastructure layer (e.g. an SQS event processor)
/// to automatically resume suspended parents once all children have finished.
/// </summary>
public interface ICoreLongRunningServiceManager
{
    /// <summary>
    /// Called when a long-running parent service has suspended, waiting for its children.
    /// Implementations should persist enough context to resume the parent later.
    /// </summary>
    /// <param name="coreEvent">The status event emitted by the suspended parent service.</param>
    Task HandleParentSuspendedAsync(CoreStatusEvent coreEvent);

    /// <summary>
    /// Called when a long-running parent service has finished (either successfully or with an error).
    /// Implementations should clean up any tracking state for this operation.
    /// </summary>
    /// <param name="coreEvent">The status event emitted by the finished parent service.</param>
    Task HandleParentFinishedAsync(CoreStatusEvent coreEvent);

    /// <summary>
    /// Called when one or more child operations have finished.
    /// If all expected children for a parent have now completed, implementations should
    /// trigger <see cref="ICoreServiceFireOnly.ResumeAsync{TService}"/> for the parent.
    /// </summary>
    /// <param name="coreEvent">Array of status events emitted by the finished child services.</param>
    Task HandleChildrenFinishedAsync(CoreStatusEvent[] coreEvent);
}

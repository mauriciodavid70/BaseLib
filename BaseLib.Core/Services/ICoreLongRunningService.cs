using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Non-generic marker interface for a long-running service that can be resumed after
    /// its child operations complete. Enables type-erased resumption by message dispatchers.
    /// </summary>
    public interface ICoreLongRunningService
    {
        /// <summary>
        /// Restores persisted state and continues execution for the given <paramref name="operationId"/>.
        /// </summary>
        /// <param name="operationId">The operation identifier assigned when the service was first started.</param>
        Task<CoreResponseBase> ResumeAsync(string operationId);
    }

    /// <summary>
    /// Strongly-typed contract for a long-running service that suspends while child
    /// operations run asynchronously and resumes when they complete.
    /// Inherit from <see cref="CoreLongRunningServiceBase{TRequest,TResponse}"/> to implement.
    /// </summary>
    /// <typeparam name="TResponse">The response type returned when the service completes.</typeparam>
    public interface ICoreLongRunningService<TResponse> : ICoreLongRunningService
        where TResponse : CoreResponseBase, new()
    {
        /// <summary>
        /// Restores persisted state and continues execution, returning a strongly-typed response.
        /// </summary>
        /// <param name="operationId">The operation identifier assigned when the service was first started.</param>
        new Task<TResponse> ResumeAsync(string operationId);
    }
}
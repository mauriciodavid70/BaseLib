using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Dispatches service invocations asynchronously without waiting for the result.
    /// Used by <see cref="CoreLongRunningServiceBase{TRequest,TResponse}"/> to spawn
    /// child service calls that run independently while the parent suspends.
    /// </summary>
    public interface ICoreServiceFireOnly
    {
        /// <summary>
        /// Fires a strongly-typed service invocation without awaiting its result.
        /// </summary>
        /// <typeparam name="TService">The service type to invoke.</typeparam>
        /// <param name="request">The request payload.</param>
        /// <param name="correlationId">Optional correlation identifier to link related operations.</param>
        /// <param name="isLongRunningChild"><see langword="true"/> when called from a long-running parent service.</param>
        Task FireAsync<TService>(CoreRequestBase request, string? correlationId = null, bool isLongRunningChild = false)
            where TService : ICoreServiceBase;

        /// <summary>
        /// Fires a service invocation by assembly-qualified type name without awaiting its result.
        /// </summary>
        /// <param name="typeName">Assembly-qualified type name of the service to invoke.</param>
        /// <param name="request">The request payload.</param>
        /// <param name="correlationId">Optional correlation identifier.</param>
        /// <param name="isLongRunningChild"><see langword="true"/> when called from a long-running parent service.</param>
        Task FireAsync(string typeName, CoreRequestBase request, string? correlationId = null, bool isLongRunningChild = false);

        /// <summary>
        /// Fires multiple requests against the same service type in a single batch without awaiting results.
        /// </summary>
        /// <typeparam name="TService">The service type to invoke for each request.</typeparam>
        /// <param name="requests">Collection of request payloads.</param>
        /// <param name="correlationId">Optional correlation identifier shared by all requests in the batch.</param>
        /// <param name="isLongRunningChild"><see langword="true"/> when called from a long-running parent service.</param>
        Task FireManyAsync<TService>(IEnumerable<CoreRequestBase> requests, string? correlationId = null, bool isLongRunningChild = false)
            where TService : ICoreServiceBase;

        /// <summary>
        /// Resumes a suspended long-running service by its <paramref name="operationId"/>.
        /// </summary>
        /// <typeparam name="TService">The long-running service type to resume.</typeparam>
        /// <param name="operationId">The operation identifier assigned when the service was first started.</param>
        /// <param name="correlationId">Optional correlation identifier.</param>
        Task ResumeAsync<TService>(string operationId, string? correlationId = null)
            where TService : ICoreLongRunningService;

        /// <summary>
        /// Resumes a suspended long-running service identified by assembly-qualified type name.
        /// </summary>
        /// <param name="typeName">Assembly-qualified type name of the long-running service.</param>
        /// <param name="operationId">The operation identifier assigned when the service was first started.</param>
        /// <param name="correlationId">Optional correlation identifier.</param>
        Task ResumeAsync(string typeName, string operationId, string? correlationId = null);
    }
}
using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Non-generic marker interface for a core service. Enables type-erased invocation when the
    /// concrete request/response types are not known at compile time (e.g. message dispatchers).
    /// </summary>
    public interface ICoreServiceBase
    {
        /// <summary>
        /// Executes the service with a base-typed request and returns a base-typed response.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="correlationId">Optional correlation identifier used to group related operations.</param>
        /// <param name="isLongRunningChild">
        /// <see langword="true"/> when this invocation is a child spawned by a
        /// <see cref="CoreLongRunningServiceBase{TRequest,TResponse}"/>; <see langword="false"/> otherwise.
        /// </param>
        Task<CoreResponseBase> RunAsync(CoreRequestBase request, string? correlationId = null, bool isLongRunningChild = false);
    }

    /// <summary>
    /// Strongly-typed contract for a core service that processes a <typeparamref name="TRequest"/>
    /// and returns a <typeparamref name="TResponse"/>.
    /// Inherit from <see cref="CoreServiceBase{TRequest,TResponse}"/> to implement.
    /// </summary>
    /// <typeparam name="TRequest">The request type, derived from <see cref="CoreRequestBase"/>.</typeparam>
    /// <typeparam name="TResponse">The response type, derived from <see cref="CoreResponseBase"/>.</typeparam>
    public interface ICoreServiceBase<TRequest, TResponse> : ICoreServiceBase
        where TRequest : CoreRequestBase
        where TResponse : CoreResponseBase, new()
    {
        /// <summary>
        /// Executes the service with the strongly-typed <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The strongly-typed request to process.</param>
        /// <param name="correlationId">Optional correlation identifier used to group related operations.</param>
        /// <param name="isLongRunningChild">
        /// <see langword="true"/> when this invocation is a child spawned by a
        /// long-running service; <see langword="false"/> otherwise.
        /// </param>
        Task<TResponse> RunAsync(TRequest request, string? correlationId = null, bool isLongRunningChild = false);
    }
}
using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Resolves and executes services by assembly-qualified type name.
    /// Used by message processors to dispatch incoming messages to the correct service
    /// without compile-time knowledge of the concrete type.
    /// </summary>
    public interface ICoreServiceRunner
    {
        /// <summary>
        /// Resolves the service identified by <paramref name="typeName"/> and runs it with the given request.
        /// </summary>
        /// <param name="typeName">Assembly-qualified type name of the service to run.</param>
        /// <param name="request">The request payload.</param>
        /// <param name="correlationId">Optional correlation identifier.</param>
        /// <param name="IsLongRunningChild"><see langword="true"/> when this is a child spawned by a long-running parent.</param>
        Task<CoreResponseBase> RunAsync(string typeName, CoreRequestBase request, string? correlationId = null, bool IsLongRunningChild = false);

        /// <summary>
        /// Resolves the long-running service identified by <paramref name="typeName"/> and resumes it.
        /// </summary>
        /// <param name="typeName">Assembly-qualified type name of the long-running service to resume.</param>
        /// <param name="operationId">The operation identifier assigned when the service was first started.</param>
        Task<CoreResponseBase> ResumeAsync(string typeName, string operationId);
    }
}

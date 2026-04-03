namespace BaseLib.Core.Services
{
    /// <summary>
    /// Persists and retrieves the field-level state of a suspended
    /// <see cref="CoreLongRunningServiceBase{TRequest,TResponse}"/> so it can be
    /// restored when all child operations have completed.
    /// The AWS implementation stores state as JSON in S3 via <c>S3CoreServiceStateStore</c>.
    /// </summary>
    public interface ICoreServiceStateStore
    {
        /// <summary>
        /// Persists the service state snapshot keyed by <paramref name="operationId"/>.
        /// </summary>
        /// <param name="operationId">Unique identifier for the suspended operation.</param>
        /// <param name="state">Dictionary of field names to their current values.</param>
        Task WriteAsync(string operationId, IDictionary<string, object?> state);

        /// <summary>
        /// Retrieves the previously persisted state for the given <paramref name="operationId"/>.
        /// </summary>
        /// <param name="operationId">Unique identifier for the suspended operation.</param>
        /// <returns>Dictionary of field names to their stored values.</returns>
        Task<IDictionary<string, object?>> ReadAsync(string operationId);
    }
}
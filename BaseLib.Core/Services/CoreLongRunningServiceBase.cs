using System.Reflection;
using BaseLib.Core.Models;
using FluentValidation;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Base class for long-running (suspendable/resumable) services. Extends
    /// <see cref="CoreServiceBase{TRequest,TResponse}"/> with the ability to fire child services
    /// asynchronously, suspend execution while children run, and resume once all children finish.
    /// Implement <see cref="CoreServiceBase{TRequest,TResponse}.RunAsync()"/> to start the workflow
    /// and <see cref="ResumeAsync()"/> to finalise it after children complete.
    /// </summary>
    /// <typeparam name="TRequest">The strongly-typed request object for this service.</typeparam>
    /// <typeparam name="TResponse">The strongly-typed response object for this service.</typeparam>
    public abstract partial class CoreLongRunningServiceBase<TRequest, TResponse> : CoreServiceBase<TRequest, TResponse>, ICoreLongRunningService<TResponse>
        where TRequest : CoreRequestBase
        where TResponse : CoreResponseBase, new()
    {
        private readonly ICoreServiceFireOnly fireOnly;
        private readonly ICoreServiceStateStore stateStore;
        private int childrenCount = 0; // This will be accessed with Interlocked methods for thread safety

        /// <summary>Initializes the long-running service base with its required dependencies.</summary>
        /// <param name="invoker">Fire-only dispatcher used to enqueue child service executions.</param>
        /// <param name="stateStore">Store used to persist and restore service state across suspension/resume cycles.</param>
        /// <param name="validator">Optional FluentValidation validator for the request.</param>
        /// <param name="eventSink">Optional status-event sink. Defaults to <see cref="NullCoreEventSink"/>.</param>
        public CoreLongRunningServiceBase(ICoreServiceFireOnly invoker, ICoreServiceStateStore stateStore, IValidator<TRequest>? validator = null, ICoreStatusEventSink? eventSink = null)
            : base(validator, eventSink)
        {
            this.fireOnly = invoker;
            this.stateStore = stateStore;
        }

        /// <summary>
        /// El estado del servicio es suspended, el finalize guarda el estado del servicio.
        /// </summary>
        protected override async Task FinalizeAsync()
        {
            if (this.childrenCount > 0)
            {
                //el servicio es de larga duración y tiene tareas asincrónicas, se suspende.
                this.Status = CoreServiceStatus.Suspended;

                //aquí agregamos el reasoncode de suspended al codigo existente
                this.Response!.ReasonCode = ((CoreServiceReasonCode)this.Response.ReasonCode.Value) | CoreServiceReasonCode.Suspended;

                //hay tareas asincrónicas, debe guardar el estado serializado.
                var state = this.GetState();
                await this.stateStore.WriteAsync(this.OperationId!, state);
            }

            // Reporta el evento de suspendido al sink de eventos
            await this.OnWriteStatusEventAsync();
        }

        /// <inheritdoc/>
        protected override CoreStatusEvent GetStatusEvent()
        {
            var statusEvent = base.GetStatusEvent();
            statusEvent.IsLongRunningService = this.childrenCount > 0;
            statusEvent.ChildrenCount = this.childrenCount;
            return statusEvent;
        }

        async Task<CoreResponseBase> ICoreLongRunningService.ResumeAsync(string operationId)
        {
            var response = await this.ResumeAsync(operationId);
            return response;
        }

        /// <summary>
        /// Reanuda el proceso de larga duración. Invocado por el worker cuando todos los procesos secundarios han finalizado.
        /// </summary>
        public virtual async Task<TResponse> ResumeAsync(string operationId)
        {
            if (string.IsNullOrEmpty(operationId))
                throw new ArgumentNullException(nameof(operationId));
            try
            {
                var state = await this.stateStore.ReadAsync(operationId);
                this.SetState(state);

                this.Response = await this.ResumeAsync();

                //always set the OperationId
                this.Response.OperationId = this.OperationId;

                // If the ReasonCode is still Undefined, set it based on Succeeded
                if (this.Response.ReasonCode == CoreServiceReasonCode.Undefined)
                {
                    this.Response.ReasonCode = this.Response.Succeeded ? CoreServiceReasonCode.Succeeded : CoreServiceReasonCode.Failed;
                }
            }
            catch (Exception ex)
            {
                this.Response = new TResponse
                {
                    Succeeded = false,
                    ReasonCode = CoreServiceReasonCode.ExceptionHappened,
                    Messages = [
                        $"Exception of type {ex.GetType().Name} on {this.GetType().Name} with message {ex.Message} Happened",
                        ex.StackTrace ?? "No StackTrace in exception"
                    ]
                };

            }
            finally
            {
                this.Status = CoreServiceStatus.Finished;

                this.FinishedOn = DateTimeOffset.UtcNow;

                await this.OnWriteStatusEventAsync();
            }

            return this.Response;

        }

        /// <summary>Contains the resumption logic executed after all child services have completed. Implemented by derived classes.</summary>
        /// <returns>The final response produced when the long-running workflow completes.</returns>
        protected abstract Task<TResponse> ResumeAsync();

        /// <summary>
        /// Enqueues a single child service for asynchronous execution and increments the internal children counter.
        /// The parent service will be resumed once all enqueued children finish.
        /// </summary>
        /// <typeparam name="TService">Type of the child service to fire.</typeparam>
        /// <param name="request">Request payload for the child service.</param>
        /// <param name="correlationId">Correlation ID to pass to the child. Defaults to this service's <see cref="CoreServiceBase{TRequest,TResponse}.OperationId"/>.</param>
        public virtual Task FireAsync<TService>(CoreRequestBase request, string? correlationId = null)
            where TService : ICoreServiceBase
        {
            // Increment the children count in a thread-safe manner
            Interlocked.Increment(ref this.childrenCount);
            return this.fireOnly.FireAsync<TService>(request, correlationId ?? this.OperationId, isLongRunningChild: true);
        }

        /// <summary>
        /// Enqueues multiple child services for asynchronous batch execution and increments the internal children counter by the batch size.
        /// The parent service will be resumed once all enqueued children finish.
        /// </summary>
        /// <typeparam name="TService">Type of the child service to fire.</typeparam>
        /// <param name="requests">Collection of request payloads, one per child service invocation.</param>
        /// <param name="correlationId">Correlation ID to pass to the children. Defaults to this service's <see cref="CoreServiceBase{TRequest,TResponse}.OperationId"/>.</param>
        public virtual Task FireManyAsync<TService>(IEnumerable<CoreRequestBase> requests, string? correlationId = null)
            where TService : ICoreServiceBase
        {
            // Get count and use Interlocked for thread safety
            int count = requests.Count();
            Interlocked.Add(ref this.childrenCount, count);
            return this.fireOnly.FireManyAsync<TService>(requests, correlationId ?? this.OperationId, isLongRunningChild: true);
        }

        /// <summary>
        /// Captures the full instance-field graph of this service (including inherited private fields)
        /// as a dictionary suitable for passing to <see cref="ICoreServiceStateStore.WriteAsync"/>.
        /// Read-only and compiler-generated backing fields are excluded.
        /// </summary>
        /// <returns>Dictionary mapping field names to their current values.</returns>
        protected IDictionary<string, object?> GetState()
        {
            var dict = new Dictionary<string, object?>();
            var type = this.GetType();

            // Traverse type hierarchy to include inherited private fields
            while (type != null && type != typeof(object))
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    // Exclude readonly fields and backing fields (fields with names like <PropertyName>k__BackingField)
                    if (field.IsInitOnly || field.Name.Contains("k__BackingField"))
                        continue;

                    dict[field.Name] = field.GetValue(this);
                }

                type = type.BaseType;
            }

            return dict;
        }

        /// <summary>
        /// Restores the instance-field graph from a previously captured state dictionary,
        /// reconstructing the service to the same in-memory state it had when it was suspended.
        /// </summary>
        /// <param name="state">Dictionary produced by <see cref="GetState"/>.</param>
        protected void SetState(IDictionary<string, object?> state)
        {
            var type = this.GetType();

            // Traverse type hierarchy to include inherited private fields
            while (type != null && type != typeof(object))
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    // Exclude readonly fields and backing fields
                    if (field.IsInitOnly || field.Name.Contains("k__BackingField"))
                        continue;

                    if (state.TryGetValue(field.Name, out var value))
                    {
                        field.SetValue(this, value);
                    }
                }

                type = type.BaseType;
            }
        }
    }
}
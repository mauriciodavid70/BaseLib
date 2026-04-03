using BaseLib.Core.Models;
using FluentValidation;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Base class for all Core services. Handles the request/response lifecycle, optional
    /// FluentValidation, status-event emission, and structured error handling.
    /// Derive from this class and implement <see cref="RunAsync()"/> with the business logic.
    /// </summary>
    /// <typeparam name="TRequest">The strongly-typed request object for this service.</typeparam>
    /// <typeparam name="TResponse">The strongly-typed response object for this service.</typeparam>
    public abstract partial class CoreServiceBase<TRequest, TResponse> : ICoreServiceBase<TRequest, TResponse>
         where TRequest : CoreRequestBase
         where TResponse : CoreResponseBase, new()
    {
        private string? operationId;
        private string? correlationId;
        private bool isLongRunningChild;
        private DateTimeOffset startedOn;
        private DateTimeOffset finishedOn;
        private CoreServiceStatus status;
        /// <summary>Gets or sets the current execution status of this service instance.</summary>
        protected CoreServiceStatus Status
        {
            get { return this.status; }
            set { this.status = value; }
        }

        private TRequest? request;
        /// <summary>Gets the current request. Throws <see cref="NullReferenceException"/> if accessed before <see cref="RunAsync(TRequest,string?,bool)"/> is called.</summary>
        protected TRequest Request
        {
            get { return this.request ?? throw new NullReferenceException("Request is null"); }
        }

        private TResponse? response;
        /// <summary>Gets or sets the response being built by this service execution.</summary>
        protected TResponse? Response
        {
            get { return this.response; }
            set { this.response = value; }
        }

        /// <summary>Optional FluentValidation validator applied before <see cref="RunAsync()"/> is called.</summary>
        protected IValidator<TRequest>? Validator { get; set; }
        /// <summary>Sink that receives <see cref="CoreStatusEvent"/> notifications at service start and finish.</summary>
        protected ICoreStatusEventSink EventSink { get; }

        /// <summary>Unique identifier for this service execution, assigned at the start of each run.</summary>
        protected string? OperationId { get { return this.operationId; } }
        /// <summary>Correlation identifier passed in from the caller, used to group related operations.</summary>
        protected string? CorrelationId { get { return this.correlationId; } }

        /// <summary>UTC timestamp captured when this service execution began.</summary>
        protected DateTimeOffset StartedOn { get { return this.startedOn; } }

        /// <summary>Gets or sets the UTC timestamp captured when this service execution finished.</summary>
        protected DateTimeOffset FinishedOn { get { return this.finishedOn; } set { this.finishedOn = value; } }

        /// <summary>Initializes the service with optional validation and event-sink dependencies.</summary>
        /// <param name="validator">FluentValidation validator for the request. Pass <see langword="null"/> to skip validation.</param>
        /// <param name="eventSink">Status-event sink. Defaults to <see cref="NullCoreEventSink"/> when <see langword="null"/>.</param>
        public CoreServiceBase(IValidator<TRequest>? validator = null, ICoreStatusEventSink? eventSink = null)
        {
            this.Validator = validator;
            this.EventSink = eventSink ?? new NullCoreEventSink();
        }

        /// <inheritdoc/>
        public async virtual Task<CoreResponseBase> RunAsync(CoreRequestBase request, string? correlationId = null, bool isLongRunningChild = false)
        {
            var response = await RunAsync((TRequest)request, correlationId, isLongRunningChild);
            return response;
        }

        /// <summary>
        /// Executes the service with a strongly-typed request. Validates the request (if a validator
        /// is configured), calls <see cref="RunAsync()"/>, emits status events, and handles exceptions.
        /// </summary>
        /// <param name="request">The typed request to process.</param>
        /// <param name="correlationId">Optional correlation ID to link related operations.</param>
        /// <param name="isLongRunningChild">Set to <see langword="true"/> when this service is a child of a long-running parent.</param>
        /// <returns>The typed response produced by <see cref="RunAsync()"/>.</returns>
        public async virtual Task<TResponse> RunAsync(TRequest request, string? correlationId = null, bool isLongRunningChild = false)
        {
            try
            {
                this.status = CoreServiceStatus.Started;
                this.request = request;
                this.operationId = Guid.NewGuid().ToString();
                this.correlationId = correlationId;
                this.isLongRunningChild = isLongRunningChild;
                this.startedOn = DateTimeOffset.UtcNow;

                await this.OnWriteStatusEventAsync();

                if (this.Validator != null && this.Request != null)
                {
                    var validationResult = await this.Validator.ValidateAsync(this.Request);
                    if (!validationResult.IsValid)
                    {
                        this.response = new TResponse
                        {
                            //toma el resultado y lo mapea al response
                            Succeeded = false,
                            ReasonCode = CoreServiceReasonCode.ValidationResultNotValid,
                            Messages = validationResult.Errors.Select(e => $"Validation error on {e.PropertyName} with message {e.ErrorMessage}").ToArray()
                        };
                        return this.response;
                    }
                }

                //No hay validación o la validación fue exitosa
                this.response = await RunAsync();

                //always set the OperationId
                this.response.OperationId = this.operationId;

                if (this.response.ReasonCode == CoreServiceReasonCode.Undefined)
                {
                    this.response.ReasonCode = this.response.Succeeded ? CoreServiceReasonCode.Succeeded : CoreServiceReasonCode.Failed;
                }
            }
            catch (Exception ex)
            {
                this.response = new TResponse
                {
                    OperationId = this.operationId,
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
                await this.FinalizeAsync();
            }

            return this.response;

        }

        /// <summary>
        /// Called after <see cref="RunAsync()"/> completes (or throws). Sets the status to
        /// <see cref="CoreServiceStatus.Finished"/> and emits the final status event.
        /// Override in long-running services to suspend instead of finishing.
        /// </summary>
        protected virtual Task FinalizeAsync()
        {
            this.status = CoreServiceStatus.Finished;

            this.finishedOn = DateTimeOffset.UtcNow;

            return this.OnWriteStatusEventAsync();
        }

        /// <summary>Contains the business logic for this service. Implemented by derived classes.</summary>
        /// <returns>The response produced by this service.</returns>
        protected abstract Task<TResponse> RunAsync();

        /// <summary>
        /// Builds a <see cref="CoreStatusEvent"/> and writes it to <see cref="EventSink"/>.
        /// Override to add custom logic before or after the event is emitted.
        /// </summary>
        protected virtual async Task OnWriteStatusEventAsync()
        {
            await this.EventSink.WriteAsync(this.GetStatusEvent());
        }

        /// <summary>Creates a failed response with the given reason code and optional messages.</summary>
        /// <param name="reasonCode">Domain enum value describing why the operation failed.</param>
        /// <param name="messages">Optional diagnostic messages to include in the response.</param>
        /// <returns>A new <typeparamref name="TResponse"/> with <c>Succeeded = false</c>.</returns>
        public TResponse Fail(Enum reasonCode, params string[] messages)
        {
            return new TResponse
            {
                Succeeded = false,
                ReasonCode = reasonCode,
                Messages = messages
            };
        }

        /// <summary>Creates a successful response, optionally overriding the default reason code.</summary>
        /// <param name="reasonCode">Reason code to use. Defaults to <see cref="CoreServiceReasonCode.Succeeded"/> when <see langword="null"/>.</param>
        /// <param name="messages">Optional informational messages to include in the response.</param>
        /// <returns>A new <typeparamref name="TResponse"/> with <c>Succeeded = true</c>.</returns>
        public TResponse Succeed(Enum? reasonCode = null, params string[] messages)
        {
            return new TResponse
            {
                Succeeded = true,
                ReasonCode = reasonCode ?? CoreServiceReasonCode.Succeeded
            };
        }

        /// <summary>Builds the <see cref="CoreStatusEvent"/> that describes the current execution state.</summary>
        /// <returns>A populated <see cref="CoreStatusEvent"/> ready to be written to <see cref="EventSink"/>.</returns>
        protected virtual CoreStatusEvent GetStatusEvent()
        {
            //type of the service
            var type = this.GetType();
            var assemblyName = type.Assembly.GetName().Name;

            return new CoreStatusEvent
            {
                TypeName = $"{type.FullName}, {assemblyName}",
                ModuleName = assemblyName,
                ServiceName = type.Name,
                Status = this.status,
                OperationId = this.operationId,
                CorrelationId = this.correlationId,
                StartedOn = this.startedOn,
                FinishedOn = this.finishedOn,
                Request = this.request,
                Response = this.response,
                IsLongRunningChild = this.isLongRunningChild
            };
        }

    }
}

﻿using BaseLib.Core.Models;
using FluentValidation;

namespace BaseLib.Core.Services
{
    public abstract partial class CoreServiceBase<TRequest, TResponse> : ICoreServiceBase<TRequest, TResponse>
         where TRequest : CoreRequestBase
         where TResponse : CoreResponseBase, new()
    {
        private string? operationId;
        private string? correlationId;

        private DateTimeOffset startedOn;
        private DateTimeOffset finishedOn;
        private CoreServiceStatus status;

        private TRequest? request;
        protected TRequest? Request
        {
            get { return this.request; }
        }

        private TResponse? response;
        protected TResponse? Response
        {
            get { return this.response; }
            set { this.response = value; }
        }

        protected IValidator<TRequest>? Validator { get; set; }
        protected ICoreStatusEventSink EventSink { get; }

        protected string? OperationId { get { return this.operationId; } }
        protected string? CorrelationId { get { return this.correlationId; } }

        protected DateTimeOffset StartedOn { get { return this.startedOn; } }

        protected DateTimeOffset FinishedOn { get { return this.finishedOn; } }

        public CoreServiceBase(IValidator<TRequest>? validator = null, ICoreStatusEventSink? eventSink = null)
        {
            this.Validator = validator;
            this.EventSink = eventSink ?? new NullCoreEventSink();
        }

        public async Task<CoreResponseBase> RunAsync(CoreRequestBase request, string? correlationId = null)
        {
            var response = await RunAsync((TRequest)request, correlationId);
            return response;
        }

        public async Task<TResponse> RunAsync(TRequest request, string? correlationId = null)
        {
            try
            {
                this.status = CoreServiceStatus.Started;
                this.request = request;
                this.operationId = Guid.NewGuid().ToString();
                this.correlationId = correlationId;
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

                if (this.response.ReasonCode == CoreServiceReasonCode.Undefined)
                {
                    this.response.ReasonCode = this.response.Succeeded ? CoreServiceReasonCode.Succeeded : CoreServiceReasonCode.Failed;
                }
            }
            catch (Exception ex)
            {
                this.response = new TResponse
                {
                    Succeeded = false,
                    ReasonCode = CoreServiceReasonCode.ExceptionHappened,
                    Messages = new string[] { 
                        $"Exception of type {ex.GetType().Name} on {this.GetType().Name} with message {ex.Message} Happened",
                        ex.StackTrace ?? "No StackTrace in exception"
                    }
                };
            }
            finally
            {
                this.status = CoreServiceStatus.Finished;
                this.finishedOn = DateTimeOffset.UtcNow;

                await this.OnWriteStatusEventAsync();
            }

            return this.response;

        }

        protected abstract Task<TResponse> RunAsync();

        protected virtual async Task OnWriteStatusEventAsync()
        {
            await this.EventSink.WriteAsync(this.GetStatusEvent());
        }

        public TResponse Fail(Enum reasonCode, params string[] messages)
        {
            return new TResponse
            {
                Succeeded = false,
                ReasonCode = reasonCode,
                Messages = messages
            };
        }

        public TResponse Succeed(Enum? reasonCode = null, params string[] messages)
        {
            return new TResponse
            {
                Succeeded = true,
                ReasonCode = reasonCode ?? CoreServiceReasonCode.Succeeded
            };
        }

        protected virtual CoreStatusEvent GetStatusEvent()
        {
            //type of the service
            var type = this.GetType(); 

            return new CoreStatusEvent
            {
                ModuleName = type.Assembly.GetName().Name,
                ServiceName = type.Name,
                Status = this.status,
                OperationId = this.operationId,
                CorrelationId = this.correlationId,
                StartedOn = this.startedOn,
                FinishedOn = this.finishedOn,
                Request = this.request,
                Response = this.response
            };
        }

    }
}

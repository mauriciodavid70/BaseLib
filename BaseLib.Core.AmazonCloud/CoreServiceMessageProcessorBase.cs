using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using BaseLib.Core.Services;

namespace BaseLib.Core.AmazonCloud
{
    /// <summary>
    /// AWS Lambda handler that processes an SQS batch of service-dispatch messages.
    /// Each message body is a JSON payload containing a service type name and either a
    /// <c>RunAsync</c> request or a <c>ResumeAsync</c> operation ID.
    /// Failed individual messages are returned as batch item failures so SQS can retry them.
    /// Derive from this class and register it as your Lambda function handler.
    /// </summary>
    public class CoreServiceMessageProcessorBase
    {
        private readonly ICoreServiceRunner runner;

        /// <summary>Initializes the processor with the service runner used to dispatch messages.</summary>
        /// <param name="runner">Runner that resolves and executes services by type name.</param>
        public CoreServiceMessageProcessorBase(ICoreServiceRunner runner)
        {
            this.runner = runner;
        }

        /// <summary>
        /// Lambda entry point. Processes all records in <paramref name="sqsEvent"/> concurrently
        /// and returns failed message IDs as batch item failures.
        /// </summary>
        public virtual async Task<SQSBatchResponse> HandleAsync(SQSEvent sqsEvent, ILambdaContext context)
        {
            var processingTasks = new Dictionary<string, Task>();

            foreach (var message in sqsEvent.Records)
            {
                processingTasks[message.MessageId] = HandleSingleMessageAsync(message);
            }

            try
            {
                await Task.WhenAll(processingTasks.Values);
            }
            catch (Exception ex)
            {
                // Added logging for better visibility of errors during message processing in Lambda.
                context.Logger.LogLine($"Error processing messages: {ex}");
            }

            var batchItemFailures = processingTasks
                .Where(t => t.Value.IsFaulted)
                .Select(f => new SQSBatchResponse.BatchItemFailure { ItemIdentifier = f.Key })
                .ToList();

            return new SQSBatchResponse
            {
                BatchItemFailures = batchItemFailures
            };
        }

        private async Task HandleSingleMessageAsync(SQSEvent.SQSMessage message)
        {
            var payload = CoreSerializer.Deserialize<Payload>(message.Body)
                ?? throw new NullReferenceException("No Service Name on payload");

            var typeName = payload.TypeName
                ?? throw new NullReferenceException("No Service Name on payload");

            if (string.IsNullOrEmpty(payload.Method) || payload.Method.Equals("RunAsync", StringComparison.OrdinalIgnoreCase))
            {
                var request = payload.Request ?? throw new NullReferenceException("No Request on payload");
                await runner.RunAsync(payload.TypeName, payload.Request, payload.CorrelationId, payload.IsLongRunningChild);
            }
            else if (payload.Method.Equals("ResumeAsync", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(payload.OperationId))
                    throw new NullReferenceException("No OperationId on payload");
                await runner.ResumeAsync(typeName, payload.OperationId!);
            }
            else
            {
                throw new NotSupportedException($"Method '{payload.Method}' is not supported.");
            }
        }

        private class Payload
        {
            public string? TypeName { get; set; }
            public CoreRequestBase? Request { get; set; }
            public string? OperationId { get; set; }
            public string? CorrelationId { get; set; }
            public bool IsLongRunningChild { get; set; }
            public string? Method { get; set; }
        }
       
   }
}
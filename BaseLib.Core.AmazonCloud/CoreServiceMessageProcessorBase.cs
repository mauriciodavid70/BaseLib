using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using BaseLib.Core.Services;

namespace BaseLib.Core.AmazonCloud
{
    public class CoreServiceMessageProcessorBase
    {
        private readonly ICoreServiceRunner runner;

        public CoreServiceMessageProcessorBase(ICoreServiceRunner runner)
        {
            this.runner = runner;
        }

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
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
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
        private readonly CoreMessageDispatcher dispatcher;

        /// <summary>Initializes the processor with the dispatcher used to route messages.</summary>
        /// <param name="dispatcher">Transport-agnostic dispatcher that deserializes and routes payloads.</param>
        public CoreServiceMessageProcessorBase(CoreMessageDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        /// <summary>
        /// Lambda entry point. Processes all records in <paramref name="sqsEvent"/> concurrently
        /// and returns failed message IDs as batch item failures.
        /// </summary>
        /// <param name="sqsEvent">The SQS batch event received by the Lambda function.</param>
        /// <param name="context">The Lambda execution context.</param>
        /// <returns>
        /// An <see cref="SQSBatchResponse"/> whose <c>BatchItemFailures</c> list contains the IDs
        /// of any messages that could not be dispatched successfully.
        /// </returns>
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

        private Task HandleSingleMessageAsync(SQSEvent.SQSMessage message)
        {
            var envelope = new SqsMessageEnvelope(message.Body, message.MessageId);
            return dispatcher.DispatchAsync(envelope);
        }

        private sealed record SqsMessageEnvelope(string Body, string MessageId) : ICoreMessageEnvelope;
    }
}

using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;

namespace BaseLib.Core.AmazonCloud
{
    /// <summary>
    /// Base class for handling SQS events containing SNS notifications with CoreStatusEvent payloads.
    /// </summary>
    public abstract class SqsCoreStatusEventsListenerBase
    {
        /// <summary>Lambda entry point. Deserialises SNS-wrapped status events and dispatches each to <see cref="HandleStatusEventAsync"/>.</summary>
        public virtual async Task<SQSBatchResponse> HandleAsync(SQSEvent sqsEvent, ILambdaContext context)
        {
            var events = MapEvents(sqsEvent);

            string[] failedEventIds = await HandleAsync(events);

            var batchItemFailures = failedEventIds.Select(id => new SQSBatchResponse.BatchItemFailure { ItemIdentifier = id }).ToList();

            return new SQSBatchResponse
            {
                BatchItemFailures = batchItemFailures
            };
        }

        /// <summary>Processes a pre-parsed dictionary of message-ID-to-event entries concurrently and returns the IDs of failed messages.</summary>
        protected virtual async Task<string[]> HandleAsync(Dictionary<string, CoreStatusEvent> events)
        {
            var processingTasks = new Dictionary<string, Task>();
            foreach (var kvp in events)
            {
                processingTasks[kvp.Key] = HandleStatusEventAsync(kvp.Value);
            }

            try
            {
                await Task.WhenAll(processingTasks.Values);
            }
            catch
            {
                // Intentionally swallowing exceptions to allow batch failure handling below.
            }

            var listOfFailedIds = processingTasks
                .Where(t => t.Value.IsFaulted)
                .Select(i => i.Key)
                .ToArray();

            return listOfFailedIds;

        }

        /// <summary>Deserialises SNS notifications from the SQS batch into a message-ID-to-event dictionary.</summary>
        protected virtual Dictionary<string, CoreStatusEvent> MapEvents(SQSEvent sqsEvent)
        {
            var events = new Dictionary<string, CoreStatusEvent>();

            foreach (var message in sqsEvent.Records)
            {
                var snsNotification = JsonSerializer.Deserialize<SnsNotification>(message.Body);
                if (snsNotification?.Message == null)
                    throw new NullReferenceException("No Message on SNS Notification");

                var coreEvent = CoreSerializer.Deserialize<CoreStatusEvent>(snsNotification.Message)
                    ?? throw new NullReferenceException("No CoreStatusEvent on SNS Notification");

                events[message.MessageId] = coreEvent;
            }
            return events;
        }

        /// <summary>Override to implement custom handling for each deserialised <see cref="CoreStatusEvent"/>.</summary>
        protected abstract Task HandleStatusEventAsync(CoreStatusEvent coreEvent);

        private class SnsNotification
        {
            public string? Message { get; set; }
        }

    }
}

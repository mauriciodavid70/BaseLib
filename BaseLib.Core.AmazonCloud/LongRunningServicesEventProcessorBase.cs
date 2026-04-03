using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;


namespace BaseLib.Core.Services.AmazonCloud
{
    /// <summary>
    /// AWS Lambda handler that processes SQS batches of SNS-wrapped <see cref="CoreStatusEvent"/> messages
    /// and drives the long-running service lifecycle via <see cref="ICoreLongRunningServiceManager"/>.
    /// Events are grouped by correlation ID and processed in order: suspended parents first,
    /// then finished children, then finished parents.
    /// Derive from this class and register it as your Lambda function handler.
    /// </summary>
    public class LongRunningServicesEventProcessorBase
    {
        private readonly ICoreLongRunningServiceManager longRunningServiceManager;

        /// <summary>Initializes the processor with the manager that coordinates long-running service state.</summary>
        /// <param name="longRunningServiceManager">Manager that handles parent/child lifecycle events.</param>
        public LongRunningServicesEventProcessorBase(ICoreLongRunningServiceManager longRunningServiceManager)
        {
            this.longRunningServiceManager = longRunningServiceManager;
        }

        private class SnsNotification
        {
            public string? Message { get; set; }
        }

        /// <summary>
        /// Lambda entry point. Deserialises SNS notifications, groups events by correlation ID,
        /// and delegates to <see cref="ICoreLongRunningServiceManager"/>. Returns failed message IDs
        /// as batch item failures.
        /// </summary>
        public virtual async Task<SQSBatchResponse> HandleAsync(SQSEvent sqsEvent, ILambdaContext context)
        {
            var groupsOfEvents = ExtractGroupsOfEvents(sqsEvent);

            var processingTasks = new Dictionary<string, Task>();

            //start processing each group of events each in a single thread
            foreach (var group in groupsOfEvents)
            {
                processingTasks.Add(group.Key, ProcessSingleGroupOfEventsAsync(group.Key, group.Value.Select(e => e.Event).ToArray()));
            }

            try
            {
                await Task.WhenAll(processingTasks.Values);
            }
            catch
            {
                // Intentionally swallowing exceptions to allow batch failure handling below.
            }

            // Collect any failed tasks
            var batchItemFailures = processingTasks
                .Where(t => t.Value.IsFaulted)
                .SelectMany(t =>
                {
                    // Find all message IDs associated with this task
                    var failedMessageIds = groupsOfEvents.ContainsKey(t.Key) ? groupsOfEvents[t.Key].Select(e => e.MessageId) : [];
                    return failedMessageIds.Select(id => new SQSBatchResponse.BatchItemFailure { ItemIdentifier = id });
                })
                .ToList();

            // Return the batch response with failures
            return new SQSBatchResponse
            {
                BatchItemFailures = batchItemFailures
            };

        }


        private async Task ProcessSingleGroupOfEventsAsync(string groupId, CoreStatusEvent[] events)
        {
            // Process parent events with status is Suspended
            var suspendedEvents = events
                .Where(e => e != null && e.IsLongRunningService && e.Status == CoreServiceStatus.Suspended)
                .Cast<CoreStatusEvent>()
                .ToArray();

            // Launch processing of parent suspended events
            if (suspendedEvents.Length > 0)
            {
                foreach (var statusEvent in suspendedEvents)
                {
                    await longRunningServiceManager.HandleParentSuspendedAsync(statusEvent);
                }
            }

            // Now process the children events where status is finished
            var childFinishedEvents = events
                .Where(e => e != null && e.Status == CoreServiceStatus.Finished && e.IsLongRunningChild)
                .Cast<CoreStatusEvent>()
                .ToArray();

            if (childFinishedEvents.Length > 0)
            {
                //group events by correlation id
                var batchOfChildFinishedEvents = childFinishedEvents
                    .GroupBy(e => e.CorrelationId)
                    .ToArray();

                foreach (var batch in batchOfChildFinishedEvents)
                {
                    //invoke service manager with each batch in parallel
                    await longRunningServiceManager.HandleChildrenFinishedAsync(batch.ToArray());
                }
            }

            // Process parent events with status is Finished
            var finishedEvents = events
                .Where(e => e != null && e.IsLongRunningService && e.Status == CoreServiceStatus.Finished)
                .Cast<CoreStatusEvent>()
                .ToArray();

            // Handle parent finished events in parallel
            if (finishedEvents.Length > 0)
            {
                foreach (var statusEvent in finishedEvents)
                {
                    await longRunningServiceManager.HandleParentFinishedAsync(statusEvent);
                }
            }
        }

        private Dictionary<string, List<(CoreStatusEvent Event, string MessageId)>> ExtractGroupsOfEvents(SQSEvent sqsEvent)
        {
            var setOfEvents = new Dictionary<string, List<(CoreStatusEvent Event, string MessageId)>>();

            // Deserialize Core Status Events and track MessageId, skip if snsNotification.Message is null
            var entries = sqsEvent.Records.Select(record =>
            {
                var snsNotification = JsonSerializer.Deserialize<SnsNotification>(record.Body);
                if (snsNotification?.Message == null)
                    return (Event: null, MessageId: record.MessageId);

                var coreEvent = CoreSerializer.Deserialize<CoreStatusEvent>(snsNotification.Message);
                return (Event: coreEvent, MessageId: record.MessageId);
            })
            .Where(entry => entry.Event != null);

            foreach (var (statusEvent, messageId) in entries)
            {
                string evtKey = statusEvent!.CorrelationId ?? statusEvent.OperationId
                    ?? throw new NullReferenceException("Both CorrelationId and OperationId are null in the event.");

                if (!setOfEvents.ContainsKey(evtKey))
                {
                    setOfEvents[evtKey] = new List<(CoreStatusEvent Event, string MessageId)>();
                }

                setOfEvents[evtKey].Add((statusEvent, messageId));
            }

            return setOfEvents;
        }

    }

}


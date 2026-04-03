using BaseLib.Core.Models;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BaseLib.Core.Serialization;

namespace BaseLib.Core.Services.AmazonCloud
{
    /// <summary>
    /// Sns Implementation of ICoreStatusEventSink, Writes serialized event to an AWS SNS Topic
    /// Use as a singleton is suggested.
    /// </summary>
    public class SnsCoreStatusEventSink : ICoreStatusEventSink
    {
        private readonly IAmazonSimpleNotificationService sns;
        private readonly string topicName;
        private readonly bool isFifo;
        private Topic? topic;
        private readonly SemaphoreSlim initializationSemaphore = new SemaphoreSlim(1, 1);
        private bool isInitialized = false;

        /// <summary>Initializes the sink.</summary>
        /// <param name="sns">SNS service client.</param>
        /// <param name="topicName">Plain topic name (not ARN). Append <c>.fifo</c> for FIFO topics.</param>
        public SnsCoreStatusEventSink(IAmazonSimpleNotificationService sns, string topicName)
        {
            this.sns = sns;
            this.topicName = topicName;
            this.isFifo = topicName.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes the SNS topic asynchronously, ensuring it only happens once
        /// </summary>
        /// <returns>A Task representing the asynchronous operation</returns>
        private async Task InitializeAsync()
        {
            if (!isInitialized)
            {
                try
                {
                    await initializationSemaphore.WaitAsync();
                    if (!isInitialized)
                    {
                        this.topic = await this.sns.FindTopicAsync(this.topicName);
                        isInitialized = true;
                    }
                }
                finally
                {
                    initializationSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task WriteAsync(CoreStatusEvent statusEvent)
        {
            await InitializeAsync();

            var message = CoreSerializer.Serialize(statusEvent);

            var response = statusEvent.Response;

            var request = new PublishRequest
            {
                Message = message,
                TopicArn = topic!.TopicArn,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "ModuleName",  new MessageAttributeValue{ DataType = "String", StringValue = statusEvent.ModuleName } },
                    { "ServiceName",  new MessageAttributeValue{ DataType = "String", StringValue = statusEvent.ServiceName } },
                    { "Status",  new MessageAttributeValue{ DataType = "String", StringValue = statusEvent.Status.ToString() } },
                    { "Succeeded",  new MessageAttributeValue{ DataType = "String", StringValue = response != null ? response.Succeeded.ToString() : "False" } },
                    { "ReasonCode",  new MessageAttributeValue{ DataType = "String", StringValue = response != null ? response.ReasonCode.Value.ToString() : "0" } },
                    { "IsLongRunningService",  new MessageAttributeValue{ DataType = "String", StringValue = statusEvent.IsLongRunningService.ToString() } },
                    { "IsLongRunningChild",  new MessageAttributeValue{ DataType = "String", StringValue = statusEvent.IsLongRunningChild.ToString() } }
                }
            };

            // Set FIFO-specific attributes if this is a FIFO topic
            if (this.isFifo)
            {
                request.MessageGroupId = statusEvent.CorrelationId ?? statusEvent.OperationId;
                request.MessageDeduplicationId = $"{statusEvent.OperationId}-{statusEvent.Status}";
            }

            await this.sns.PublishAsync(request);
        }
    }
}


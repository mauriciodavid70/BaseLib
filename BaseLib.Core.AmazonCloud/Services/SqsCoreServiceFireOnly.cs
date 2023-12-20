using System.Text.Json;
using Amazon.SQS;
using BaseLib.Core.Models;

namespace BaseLib.Core.Services.AmazonCloud
{
    public class SqsCoreServiceFireOnly : ICoreServiceFireOnly
    {
        private readonly IAmazonSQS sqs;
        private readonly string queueName;

        public SqsCoreServiceFireOnly(IAmazonSQS sqs, string queueName)
        {
            this.sqs = sqs;
            this.queueName = queueName;
        }

        public async Task FireAsync<TService>(CoreServiceRequestBase request, string? correlationId = null)
            where TService : ICoreServiceBase
        {
            var r = await sqs.GetQueueUrlAsync(this.queueName);
            var queueUrl = r.QueueUrl;

            var message = new
            {
                Service = typeof(TService).FullName,
                Request = request,
                CorrelationId = correlationId
            };

            var messageBody = JsonSerializer.Serialize(message);

            await this.sqs.SendMessageAsync(queueUrl, messageBody);
        }

    }
}
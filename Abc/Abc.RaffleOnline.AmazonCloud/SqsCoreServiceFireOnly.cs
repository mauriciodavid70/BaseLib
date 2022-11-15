using System.Threading.Tasks;
using Amazon.SQS;
using BaseLib.Core.Services;
using Newtonsoft.Json;

namespace Abc.RaffleOnline.AmazonCloud
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

        public async Task FireAsync<TService>(ICoreServiceRequest request) 
            where TService : ICoreServiceBase
        {
            var r = await sqs.GetQueueUrlAsync(this.queueName);
            var queueUrl = r.QueueUrl;

            var message = new
            {
                Service = typeof(TService).FullName,
                Request = request
            };

            var messageBody = JsonConvert.SerializeObject(message, Formatting.Indented);

            await this.sqs.SendMessageAsync(queueUrl, messageBody);
        }

    }
}
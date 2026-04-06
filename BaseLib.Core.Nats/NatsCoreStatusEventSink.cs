using System.Text;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using NATS.Client.Core;

namespace BaseLib.Core.Services.Nats
{
    /// <summary>
    /// <see cref="ICoreStatusEventSink"/> implementation that publishes domain status events
    /// to NATS core subjects following the hierarchy
    /// <c>{ModuleName}.{ServiceName}.succeeded</c> or <c>{ModuleName}.{ServiceName}.failed</c>.
    /// Consumers can subscribe with wildcards such as <c>orders.*.failed</c> or <c>*.*.failed</c>
    /// to replicate SNS attribute filter behaviour. Register as a singleton.
    /// </summary>
    public class NatsCoreStatusEventSink : ICoreStatusEventSink
    {
        private readonly INatsConnection nats;

        /// <summary>Initializes the event sink.</summary>
        /// <param name="nats">The NATS connection used to publish events.</param>
        public NatsCoreStatusEventSink(INatsConnection nats)
        {
            this.nats = nats;
        }

        /// <inheritdoc/>
        public async Task WriteAsync(CoreStatusEvent statusEvent)
        {
            var succeeded = statusEvent.Response?.Succeeded ?? false;
            var suffix = succeeded ? "succeeded" : "failed";
            var subject = $"{statusEvent.ModuleName}.{statusEvent.ServiceName}.{suffix}";

            var payload = Encoding.UTF8.GetBytes(CoreSerializer.Serialize(statusEvent));
            await nats.PublishAsync<byte[]>(
                subject: subject,
                data: payload,
                headers: null,
                replyTo: null,
                serializer: null,
                opts: null,
                cancellationToken: default);
        }
    }
}

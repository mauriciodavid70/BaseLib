using BaseLib.Core.Services;
using BaseLib.Core.Services.Nats;
using NATS.Client.JetStream;

namespace BasicErp.Host.Consumers;

// Single consumer reads all ERP dispatch messages — FireAsyncMessageDispatcher
// routes each to the correct service by TypeName in the FireAsyncMessage envelope.
public class ErpDispatchConsumer : NatsFireAsyncBackgroundServiceBase
{
    public ErpDispatchConsumer(FireAsyncMessageDispatcher dispatcher, INatsJSConsumer consumer)
        : base(dispatcher, consumer)
    {
    }
}

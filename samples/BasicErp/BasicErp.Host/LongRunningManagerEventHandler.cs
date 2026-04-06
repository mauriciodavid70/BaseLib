using System.Text;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using BaseLib.Core.Services;
using NATS.Client.Core;

namespace BasicErp.Host;

// Subscribes to all status events published by NatsCoreStatusEventSink
// (subject pattern: {module}.{service}.succeeded|failed) and routes them
// to ICoreLongRunningServiceManager so the long-running orchestrator resumes
// once its children have all finished.
public class LongRunningManagerEventHandler : BackgroundService
{
    private readonly INatsConnection nats;
    private readonly ICoreLongRunningServiceManager manager;
    private readonly ILogger<LongRunningManagerEventHandler> logger;

    public LongRunningManagerEventHandler(
        INatsConnection nats,
        ICoreLongRunningServiceManager manager,
        ILogger<LongRunningManagerEventHandler> logger)
    {
        this.nats = nats;
        this.manager = manager;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to all status events: {module}.{service}.succeeded and {module}.{service}.failed
        await foreach (var msg in nats.SubscribeAsync<byte[]>("*.*.*", cancellationToken: stoppingToken))
        {
            try
            {
                var json = Encoding.UTF8.GetString(msg.Data ?? Array.Empty<byte>());
                var coreEvent = CoreSerializer.Deserialize<CoreStatusEvent>(json);
                if (coreEvent == null) continue;

                if (coreEvent.Status == CoreServiceStatus.Suspended)
                {
                    await manager.HandleParentSuspendedAsync(coreEvent);
                }
                else if (coreEvent.IsLongRunningChild)
                {
                    await manager.HandleChildrenFinishedAsync([coreEvent]);
                }
                else if (coreEvent.Status == CoreServiceStatus.Finished)
                {
                    await manager.HandleParentFinishedAsync(coreEvent);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling status event on subject {Subject}", msg.Subject);
            }
        }
    }
}

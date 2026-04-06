# Design: Async Transport Packages — RabbitMQ and NATS JetStream

## Context
The `transport-abstraction` spec introduced `ICoreMessageEnvelope`, `FireAsyncMessage`, `FireAsyncMessageDispatcher`, and `FireAsyncBackgroundServiceBase` as transport-agnostic primitives in `BaseLib.Core`. This design describes how the two broker packages sit on top of those primitives.

## Dependency Graph
```
BaseLib.Core
  └── FireAsyncBackgroundServiceBase, ICoreMessageEnvelope, FireAsyncMessageDispatcher
        ├── BaseLib.Core.RabbitMQ   (RabbitMQ.Client 7.x)
        └── BaseLib.Core.Nats       (NATS.Net 2.x)
```
Neither new package references `BaseLib.Core.AmazonCloud` or `BaseLib.Core.Containers`.

## Domain Event Subject / Routing Key Design

### Mapping from SNS filter policy to broker routing

SNS clients today filter on two independent dimensions:
1. `ServiceName` (event type — e.g. `OrderPlaced`, `InventoryReserved`)
2. `Succeeded` (boolean — `true`/`false`)

These map to `CoreStatusEvent.ServiceName` and `CoreStatusEvent.Response.Succeeded`.

**RabbitMQ — topic exchange routing key:**
```
{ServiceName}.succeeded   |  OrderPlaced.succeeded
{ServiceName}.failed      |  OrderPlaced.failed
```
Consumer wildcard patterns that replicate SNS filter policies:
- `OrderPlaced.*` — all OrderPlaced events regardless of outcome
- `*.failed`      — any failed event across all services
- `#`             — all events (fanout equivalent)

**NATS — subject hierarchy:**
```
{ModuleName}.{ServiceName}.succeeded  |  orders.OrderPlaced.succeeded
{ModuleName}.{ServiceName}.failed     |  orders.OrderPlaced.failed
```
`ModuleName` comes from `CoreStatusEvent.ModuleName` (assembly name set by the framework).
Consumer wildcard patterns:
- `orders.OrderPlaced.*`  — all OrderPlaced events
- `orders.*.failed`       — any failure in the orders module
- `*.*.failed`            — all failures across all modules

`ModuleName` adds the domain scoping dimension that SNS ARNs provided implicitly through separate topic-per-module deployments.

## RabbitMQ Design Decisions

### Single channel for batch dispatch
`FireManyAsync` opens one `IChannel`, publishes all messages in a tight loop (no parallel tasks),
and disposes the channel. This avoids channel proliferation under high fan-out while keeping
the critical path simple. The RabbitMQ broker serialises writes on the TCP connection anyway.

### Abstract background service
`RabbitMqFireAsyncBackgroundServiceBase` is abstract because the queue name is application-specific.
Concrete subclasses declare the queue name and any additional queue arguments (e.g. DLX bindings).
`AddRabbitMqTransport` registers the two concrete implementations; the consumer is registered
separately via the standard `services.AddHostedService<TConsumer>()` call.

### Connection ownership
`RabbitMqCoreServiceFireOnly` and `RabbitMqCoreStatusEventSink` take `IConnection` by constructor
injection. `IConnection` is long-lived and should be registered as a singleton by the host.
The DI extension does not create the connection itself — it accepts `IConnection` from the container,
keeping connection lifecycle management outside the library.

## NATS Design Decisions

### Persistent JetStream for dispatch
`NatsCoreServiceFireOnly` uses JetStream `PublishAsync` (persistent, at-least-once) rather than
core NATS `PublishAsync` (fire-and-forget at-most-once). This matches the reliability guarantee
of SQS FIFO queues that `SqsCoreServiceFireOnly` provides.

### Pull consumer
`NatsFireAsyncBackgroundServiceBase` uses a JetStream pull consumer (`NextAsync`) rather than
a push consumer. Pull consumers are simpler to operate in containers (no push subscription
re-registration on restart), support back-pressure naturally, and are the idiomatic choice in
NATS.Net 2.x.

### INatsConnection injection
Similar to RabbitMQ, `INatsConnection` is injected; the DI extension registers implementations
only. Connection setup (server URL, credentials) is the host's responsibility.

## Options Types

### RabbitMqOptions
```csharp
ExchangeName        string   // dispatch exchange name (default: "baselib.services")
EventExchangeName   string   // event sink exchange name (default: "baselib.events")
```

### NatsTransportOptions
```csharp
StreamName          string   // JetStream stream name for service dispatch
DispatchSubject     string   // subject prefix for FireAsync messages
```
`NatsCoreStatusEventSink` derives the full subject from `CoreStatusEvent.ModuleName`
and `CoreStatusEvent.ServiceName` — no extra options required.

## Risks / Trade-offs
- RabbitMQ channel-per-publish has overhead vs. channel pooling — acceptable for typical
  service dispatch rates; revisit if benchmarks show contention.
- NATS pull consumer polls on `NextAsync` with a configurable timeout; an empty queue
  causes a tight loop. `NatsFireAsyncBackgroundServiceBase` should apply a short delay
  (e.g. 100 ms) when `ReceiveAsync` returns null to avoid busy-waiting.
- Neither package includes dead-letter or retry logic — that is transport-operator
  configuration (RabbitMQ DLX, NATS MaxDeliver).

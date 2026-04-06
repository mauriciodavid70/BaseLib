# nats-transport Specification

## Purpose
TBD - created by archiving change add-async-transports. Update Purpose after archive.
## Requirements
### Requirement: NATS Service Dispatch
`NatsCoreServiceFireOnly` SHALL implement `ICoreServiceFireOnly` by publishing `FireAsyncMessage` payloads serialized via `CoreSerializer` as persistent NATS JetStream messages to a configurable stream and subject.

#### Scenario: Single fire publishes persistent message
- **WHEN** `FireAsync<TService>(request)` is called
- **THEN** a `FireAsyncMessage` with `Method = "RunAsync"` is serialized and published via `INatsJSContext.PublishAsync` to the configured stream subject

#### Scenario: Resume publishes ResumeAsync message
- **WHEN** `ResumeAsync<TService>(operationId)` is called
- **THEN** a `FireAsyncMessage` with `Method = "ResumeAsync"` and the given `operationId` is published to the stream

### Requirement: NATS Event Sink
`NatsCoreStatusEventSink` SHALL implement `ICoreStatusEventSink` by publishing a `CoreStatusEvent` serialized via `CoreSerializer` to subject `{ModuleName}.{ServiceName}.succeeded` or `{ModuleName}.{ServiceName}.failed`, where `ModuleName` is `CoreStatusEvent.ModuleName` and `ServiceName` is `CoreStatusEvent.ServiceName`. This enables consumers to subscribe with wildcard patterns such as `orders.*.failed` or `*.*.failed` to replicate SNS attribute filter behaviour.

#### Scenario: Succeeded event published to .succeeded subject
- **WHEN** `WriteAsync(statusEvent)` is called and `statusEvent.Response.Succeeded` is `true`
- **THEN** the event is published to subject `{statusEvent.ModuleName}.{statusEvent.ServiceName}.succeeded`

#### Scenario: Failed event published to .failed subject
- **WHEN** `WriteAsync(statusEvent)` is called and `statusEvent.Response.Succeeded` is `false` (or `Response` is null)
- **THEN** the event is published to subject `{statusEvent.ModuleName}.{statusEvent.ServiceName}.failed`

### Requirement: NATS Background Service Base
`NatsFireAsyncBackgroundServiceBase` SHALL extend `FireAsyncBackgroundServiceBase` and implement `ReceiveAsync`, `AcknowledgeAsync`, and `NackAsync` for NATS JetStream. It SHALL use a durable pull consumer whose name is provided by the concrete subclass, acknowledge on successful dispatch, and nack on failure. When `ReceiveAsync` returns no message (empty queue), the base class SHALL apply a short delay before the next poll to avoid busy-waiting.

#### Scenario: Successful dispatch acknowledges message
- **WHEN** `ReceiveAsync` returns an envelope and `FireAsyncMessageDispatcher.DispatchAsync` succeeds
- **THEN** `AcknowledgeAsync` calls `AckAsync` on the NATS message

#### Scenario: Dispatch failure nacks message
- **WHEN** `FireAsyncMessageDispatcher.DispatchAsync` throws
- **THEN** `NackAsync` calls `NakAsync` on the NATS message

#### Scenario: Empty queue applies backoff delay
- **WHEN** `ReceiveAsync` returns null (no message available)
- **THEN** the consumer waits a short configurable delay before polling again

### Requirement: NATS DI Registration
`AddNatsTransport` SHALL be an `IServiceCollection` extension method that registers `NatsCoreServiceFireOnly` as `ICoreServiceFireOnly` and `NatsCoreStatusEventSink` as `ICoreStatusEventSink`. It SHALL accept an `Action<NatsTransportOptions>` to configure stream name and dispatch subject.

#### Scenario: All implementations registered
- **WHEN** `AddNatsTransport(services, options => { ... })` is called during startup
- **THEN** `ICoreServiceFireOnly` resolves to `NatsCoreServiceFireOnly` and `ICoreStatusEventSink` resolves to `NatsCoreStatusEventSink`


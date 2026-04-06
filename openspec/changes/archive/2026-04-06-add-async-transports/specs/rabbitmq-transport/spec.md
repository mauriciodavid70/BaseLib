## ADDED Requirements

### Requirement: RabbitMQ Service Dispatch
`RabbitMqCoreServiceFireOnly` SHALL implement `ICoreServiceFireOnly` by publishing `FireAsyncMessage` payloads serialized via `CoreSerializer` to a durable RabbitMQ topic exchange. The routing key SHALL equal the assembly-qualified service type name. Batch dispatch (`FireManyAsync`) SHALL publish all messages through a single channel per call.

#### Scenario: Single fire publishes to exchange
- **WHEN** `FireAsync<TService>(request)` is called
- **THEN** a `FireAsyncMessage` with `Method = "RunAsync"` is serialized and published to the configured topic exchange with routing key equal to the assembly-qualified name of `TService`

#### Scenario: Batch fire uses one channel
- **WHEN** `FireManyAsync<TService>(requests)` is called with multiple requests
- **THEN** all messages are published through a single `IChannel` and the channel is disposed after all messages are sent

#### Scenario: Resume publishes ResumeAsync message
- **WHEN** `ResumeAsync<TService>(operationId)` is called
- **THEN** a `FireAsyncMessage` with `Method = "ResumeAsync"` and the given `operationId` is published to the exchange

### Requirement: RabbitMQ Event Sink
`RabbitMqCoreStatusEventSink` SHALL implement `ICoreStatusEventSink` by publishing a `CoreStatusEvent` serialized via `CoreSerializer` to a durable RabbitMQ topic exchange. The routing key SHALL be `{ServiceName}.succeeded` when `CoreStatusEvent.Response.Succeeded` is `true`, and `{ServiceName}.failed` otherwise, enabling consumers to bind queues with wildcard patterns to replicate SNS filter policy behaviour.

#### Scenario: Succeeded event uses .succeeded routing key
- **WHEN** `WriteAsync(statusEvent)` is called and `statusEvent.Response.Succeeded` is `true`
- **THEN** the event is published with routing key `{statusEvent.ServiceName}.succeeded`

#### Scenario: Failed event uses .failed routing key
- **WHEN** `WriteAsync(statusEvent)` is called and `statusEvent.Response.Succeeded` is `false` (or `Response` is null)
- **THEN** the event is published with routing key `{statusEvent.ServiceName}.failed`

### Requirement: RabbitMQ Background Service Base
`RabbitMqFireAsyncBackgroundServiceBase` SHALL extend `FireAsyncBackgroundServiceBase` and implement `ReceiveAsync`, `AcknowledgeAsync`, and `NackAsync` for RabbitMQ. It SHALL subscribe to a durable queue whose name is provided by the concrete subclass, acknowledge the delivery on successful dispatch, and nack without requeue on unrecoverable failure.

#### Scenario: Successful dispatch acknowledges delivery
- **WHEN** `ReceiveAsync` returns an envelope and `FireAsyncMessageDispatcher.DispatchAsync` succeeds
- **THEN** `AcknowledgeAsync` calls `BasicAckAsync` on the delivery tag

#### Scenario: Dispatch failure nacks without requeue
- **WHEN** `FireAsyncMessageDispatcher.DispatchAsync` throws
- **THEN** `NackAsync` calls `BasicNackAsync` with `requeue = false`

### Requirement: RabbitMQ DI Registration
`AddRabbitMqTransport` SHALL be an `IServiceCollection` extension method that registers `RabbitMqCoreServiceFireOnly` as `ICoreServiceFireOnly` and `RabbitMqCoreStatusEventSink` as `ICoreStatusEventSink`. It SHALL accept an `Action<RabbitMqOptions>` to configure exchange names.

#### Scenario: All implementations registered
- **WHEN** `AddRabbitMqTransport(services, options => { ... })` is called during startup
- **THEN** `ICoreServiceFireOnly` resolves to `RabbitMqCoreServiceFireOnly` and `ICoreStatusEventSink` resolves to `RabbitMqCoreStatusEventSink`

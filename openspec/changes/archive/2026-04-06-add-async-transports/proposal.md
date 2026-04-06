# Change: Async Transport Packages — RabbitMQ and NATS JetStream

## Why
`BaseLib.Core.Containers` provides container-friendly implementations of all Core interfaces except the two that carry messages between services: `ICoreServiceFireOnly` (Fire-and-Forget dispatch) and `ICoreStatusEventSink` (domain event choreography). Without a broker, container deployments must still depend on AWS SQS/SNS. This change adds two new packages — `BaseLib.Core.RabbitMQ` and `BaseLib.Core.Nats` — so teams can run fully AWS-independent stacks.

## What Changes
- **New package `BaseLib.Core.RabbitMQ`** (no existing package affected):
  - `RabbitMqCoreServiceFireOnly : ICoreServiceFireOnly` — publishes `FireAsyncMessage` to a durable topic exchange; routing key = assembly-qualified service type name
  - `RabbitMqCoreStatusEventSink : ICoreStatusEventSink` — publishes `CoreStatusEvent` to a topic exchange; routing key = `{ServiceName}.succeeded` or `{ServiceName}.failed`
  - `RabbitMqFireAsyncBackgroundServiceBase : FireAsyncBackgroundServiceBase` — abstract; subclasses provide the durable queue name; acks on success, nacks without requeue on failure
  - `AddRabbitMqTransport(IServiceCollection, Action<RabbitMqOptions>)` DI extension registering the two concrete implementations

- **New package `BaseLib.Core.Nats`** (no existing package affected):
  - `NatsCoreServiceFireOnly : ICoreServiceFireOnly` — publishes `FireAsyncMessage` as a persistent JetStream message to a configurable stream and subject
  - `NatsCoreStatusEventSink : ICoreStatusEventSink` — publishes `CoreStatusEvent` to subject `{ModuleName}.{ServiceName}.succeeded|failed`
  - `NatsFireAsyncBackgroundServiceBase : FireAsyncBackgroundServiceBase` — abstract; subclasses provide the durable consumer name; pull-based JetStream consumer; acks on success, nacks on failure
  - `AddNatsTransport(IServiceCollection, Action<NatsTransportOptions>)` DI extension registering the two concrete implementations

## Impact
- Affected specs (new): `rabbitmq-transport`, `nats-transport`
- New projects: `BaseLib.Core.RabbitMQ/`, `BaseLib.Core.Nats/`
- Depends on: `transport-abstraction` spec (`FireAsyncBackgroundServiceBase`, `ICoreMessageEnvelope`, `FireAsyncMessageDispatcher`, `FireAsyncMessage`)
- **No breaking changes** — purely additive; existing Lambda/SQS/SNS consumers unchanged
- Version bump: **patch** `3.2.0 → 3.2.1` (new packages, additive only)

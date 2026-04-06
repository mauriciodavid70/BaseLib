## 1. BaseLib.Core.RabbitMQ — New Project
- [ ] 1.1 Create `BaseLib.Core.RabbitMQ/BaseLib.Core.RabbitMQ.csproj` — references `BaseLib.Core`; NuGet dep `RabbitMQ.Client 7.x`; added to `BaseLib.sln`
- [ ] 1.2 Add `RabbitMqOptions` — `ExchangeName` (default `"baselib.services"`), `EventExchangeName` (default `"baselib.events"`)
- [ ] 1.3 Implement `RabbitMqCoreServiceFireOnly : ICoreServiceFireOnly` — declares durable topic exchange on first use; publishes `FireAsyncMessage` via `CoreSerializer`; `FireManyAsync` uses a single `IChannel`
- [ ] 1.4 Implement `RabbitMqCoreStatusEventSink : ICoreStatusEventSink` — routing key `{ServiceName}.succeeded|failed`
- [ ] 1.5 Implement abstract `RabbitMqFireAsyncBackgroundServiceBase : FireAsyncBackgroundServiceBase` — `ReceiveAsync` via basic consume; `AcknowledgeAsync` → `BasicAckAsync`; `NackAsync` → `BasicNackAsync(requeue: false)`
- [ ] 1.6 Add `RabbitMqTransportExtensions.AddRabbitMqTransport(IServiceCollection, Action<RabbitMqOptions>)` — registers `ICoreServiceFireOnly` and `ICoreStatusEventSink`
- [ ] 1.7 Add XML doc comments to all public types and members (CS1591 enforcement)

## 2. BaseLib.Core.Nats — New Project
- [ ] 2.1 Create `BaseLib.Core.Nats/BaseLib.Core.Nats.csproj` — references `BaseLib.Core`; NuGet dep `NATS.Net 2.x`; added to `BaseLib.sln`
- [ ] 2.2 Add `NatsTransportOptions` — `StreamName`, `DispatchSubject`
- [ ] 2.3 Implement `NatsCoreServiceFireOnly : ICoreServiceFireOnly` — publishes `FireAsyncMessage` via `INatsJSContext.PublishAsync` (persistent)
- [ ] 2.4 Implement `NatsCoreStatusEventSink : ICoreStatusEventSink` — subject `{ModuleName}.{ServiceName}.succeeded|failed`
- [ ] 2.5 Implement abstract `NatsFireAsyncBackgroundServiceBase : FireAsyncBackgroundServiceBase` — pull consumer via `INatsJSConsumer.NextAsync`; null result applies configurable backoff delay; `AcknowledgeAsync` → `AckAsync`; `NackAsync` → `NakAsync`
- [ ] 2.6 Add `NatsTransportExtensions.AddNatsTransport(IServiceCollection, Action<NatsTransportOptions>)` — registers `ICoreServiceFireOnly` and `ICoreStatusEventSink`
- [ ] 2.7 Add XML doc comments to all public types and members (CS1591 enforcement)

## 3. Tests (BaseLib.Core.Tests)
- [ ] 3.1 Unit tests for `RabbitMqCoreServiceFireOnly` — mock `IConnection`/`IChannel`; verify routing key and `FireAsyncMessage` payload for `FireAsync`, `FireManyAsync`, `ResumeAsync`
- [ ] 3.2 Unit tests for `RabbitMqCoreStatusEventSink` — verify `{ServiceName}.succeeded` and `{ServiceName}.failed` routing keys
- [ ] 3.3 Unit tests for `NatsCoreServiceFireOnly` — mock `INatsJSContext`; verify subject and `FireAsyncMessage` payload
- [ ] 3.4 Unit tests for `NatsCoreStatusEventSink` — verify `{ModuleName}.{ServiceName}.succeeded|failed` subject construction

## 4. Version and Validation
- [ ] 4.1 Bump version in `Directory.Build.props`: `3.2.0 → 3.2.1`
- [ ] 4.2 Run `dotnet build BaseLib.sln` — must be clean (zero errors, zero CS1591 warnings)
- [ ] 4.3 Run `dotnet test` — all tests pass

# Design: Transport Abstraction Gap + Container Runtime

## Context
BaseLib services today are dispatched exclusively via AWS Lambda triggered by SQS. The dispatch logic (deserialize payload → route to `RunAsync` or `ResumeAsync`) lives inside `CoreServiceMessageProcessorBase`, which is parameterised on `SQSEvent` from the Lambda SDK. This blocks any non-Lambda deployment.

## Goals / Non-Goals
- **Goals:**
  - Extract dispatch logic into a transport-agnostic `CoreMessageDispatcher` in `BaseLib.Core`
  - Provide an abstract `CoreHostedConsumerBase` that container-based services can extend
  - Provide container-friendly implementations of `ICoreServiceStateStore`, `ICoreSecretsVault`, and `IEmailSender`
  - Keep existing Lambda/SQS behaviour 100% unchanged
- **Non-Goals:**
  - Concrete broker implementations (RabbitMQ, NATS) — covered in a follow-on proposal
  - Production-grade HA for `FileSystemCoreServiceStateStore` (single-instance use; multi-replica needs a database-backed store)

## Decisions

### IMessageEnvelope in BaseLib.Core
A minimal interface with `Body` (string) and `MessageId` (string). No transport-specific metadata bleeds into Core. The `CoreMessageDispatcher` only needs these two properties.

### CoreMessageDispatcher
Extracted verbatim from the `HandleSingleMessageAsync` private method in `CoreServiceMessageProcessorBase`. Takes `IMessageEnvelope`, deserializes via `CoreSerializer`, and calls `ICoreServiceRunner.RunAsync` or `ResumeAsync`. Stateless; safe to singleton-register.

### CoreBackgroundService
Abstract `BackgroundService`. Template method pattern:
```
ExecuteAsync → loop → ReceiveAsync() → DispatchAsync(envelope) → AcknowledgeAsync / NackAsync
```
Subclasses implement `ReceiveAsync()` (pull one or more envelopes from their transport) and `AcknowledgeAsync` / `NackAsync`. Error handling and cancellation live in the base.

### CoreServiceMessageProcessorBase (AmazonCloud refactor)
Converts `SQSEvent.SQSMessage` to `IMessageEnvelope` (anonymous inline class), delegates to `CoreMessageDispatcher`. The public `HandleAsync(SQSEvent, ILambdaContext)` signature is unchanged — no downstream breakage.

### SmtpEmailSender uses MimeKit
`IEmailSender.SendAsync` already takes a `MimeMessage` (MimeKit). `SmtpEmailSender` uses `MailKit.Net.Smtp.SmtpClient` (same library family) for SMTP delivery, keeping the dependency footprint minimal and consistent.

## Risks / Trade-offs
- `FileSystemCoreServiceStateStore` is not safe for multi-replica deployments → documented as single-instance only; operators needing HA should use the MySQL or S3 implementations
- `EnvironmentSecretsVault` exposes secrets via environment — standard container practice but requires secure secret injection (Docker secrets, K8s secrets)

## Open Questions
- None — scope is well-bounded; follow-on broker implementations are in a separate proposal

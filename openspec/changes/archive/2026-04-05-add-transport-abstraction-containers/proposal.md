# Change: Transport Abstraction Gap + Container Runtime Package

## Why
`CoreServiceMessageProcessorBase` takes `SQSEvent` directly, coupling message dispatch to AWS Lambda. Running BaseLib services in containers requires a transport-agnostic dispatch layer and concrete implementations of all Core interfaces that do not depend on cloud infrastructure.

## What Changes
- **BaseLib.Core** — add `IMessageEnvelope`, `CoreMessageDispatcher`, `CoreBackgroundService` (non-breaking, additive)
- **BaseLib.Core.AmazonCloud** — refactor `CoreServiceMessageProcessorBase` into a thin SQS adapter over `CoreMessageDispatcher` (non-breaking, same public API)
- **BaseLib.Core.Containers** (new package) — `FileSystemCoreServiceStateStore`, `EnvironmentSecretsVault`, `SmtpEmailSender`, DI extension

## Impact
- Affected specs: `transport-abstraction` (new), `containers-runtime` (new)
- Affected code:
  - `BaseLib.Core.AmazonCloud/CoreServiceMessageProcessorBase.cs` — internal refactor only
  - New: `BaseLib.Core/Services/IMessageEnvelope.cs`
  - New: `BaseLib.Core/Services/CoreMessageDispatcher.cs`
  - New: `BaseLib.Core/Services/CoreBackgroundService.cs`
  - New project: `BaseLib.Core.Containers/`
- **No breaking changes** — all existing Lambda/SQS consumers continue to work unchanged
- Version bump: **minor** `3.1.x → 3.2.0` (new package)

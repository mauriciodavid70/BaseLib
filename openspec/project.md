# Project Context

## Purpose

BaseLib is a C# (.NET 8) foundation library distributed as NuGet packages. It provides a battle-tested set of patterns and abstractions for building cloud-native backend services: request/response service orchestration, long-running async workflows, event emission, secure serialization, and AWS/MySQL integrations.

The library is consumed by downstream applications and services — it is a library, not an application. Changes that break the public API are **breaking changes** and require a proposal.

## Tech Stack

- **Language:** C# 12 / .NET 8, nullable reference types enabled
- **Package format:** NuGet (packed with `dotnet pack`)
- **Test framework:** xUnit 2.x + Moq 4.x
- **Validation:** FluentValidation 11.x
- **Email MIME:** MimeKit 4.x
- **AWS SDK:** AWSSDK v3 — SNS, SQS, S3, KMS, Secrets Manager, SES v2, Lambda
- **Database:** MySQL via `MySql.Data` 8.x
- **CI build:** `dotnet build BaseLib.sln` / `dotnet pack`

## Project Conventions

### Code Style

- Nullable reference types are **enabled** — never suppress `!` without justification.
- `ImplicitUsings` is enabled; no need for `using System;` etc.
- Use `PascalCase` for types/methods/properties; `camelCase` for locals/parameters.
- All **public** types and members require XML doc comments (`<summary>`, `<param>` for non-obvious params). The build enforces CS1591 as an error — missing docs break the build.
- Prefer `async`/`await` throughout; no `.Result` / `.Wait()` blocking calls.

### Architecture Patterns

**Dependency hierarchy (strict — no upward references):**
```
BaseLib.Core                    ← zero cloud/DB deps
├── BaseLib.Core.AmazonCloud    ← AWS implementations
├── BaseLib.Core.MySql          ← MySQL implementations
└── BaseLib.Core.Tests          ← tests (not packaged)
```

**Core Service Pattern** — all services inherit `CoreServiceBase<TRequest, TResponse>`:
- `TRequest` extends `CoreRequestBase`; `TResponse` extends `CoreResponseBase`
- Override `RunAsync()` and exit via `Fail(reasonCode)` or `Succeed()` only
- Framework handles validation, event emission, and error wrapping

**Reason Codes** — use enums with `[Description]` attributes; they implicitly convert to `CoreReasonCode`:
```csharp
public enum MyReasonCode { [Description("Not found")] NotFound = 1 }
return Fail(MyReasonCode.NotFound, "extra context");
```

**Long-Running Services** — inherit `CoreLongRunningServiceBase<TRequest, TResponse>`:
- Fire children with `await FireAsync<TChildService>(childRequest)` or `FireManyAsync(...)`
- Parent **suspends** after firing; state is persisted via `ICoreServiceStateStore` (S3)
- Implement `ResumeAsync()` — called by `ICoreLongRunningServiceManager` (MySQL) when all children complete

**Serialization:**
- `CoreSerializer` — polymorphic JSON using `___type` discriminator
- `CoreSecureJsonSerializer` — encrypts fields marked `[CoreSecret]` using envelope encryption
- `ICoreSecretsVault` — key management abstraction (AWS Secrets Manager implementation included)

**SQS/Lambda entry point:** `CoreServiceMessageProcessorBase` (AmazonCloud) is the standard Lambda handler for SQS-triggered services.

### Testing Strategy

- Unit tests live in `BaseLib.Core.Tests` (xUnit + Moq).
- Test interfaces and abstractions, not AWS/MySQL implementations directly.
- Run all tests: `dotnet test`
- Run targeted tests: `dotnet test --filter "FullyQualifiedName~SomeTestClass"`
- No integration tests against real AWS/MySQL in this repo — mock via interfaces.

### Git Workflow

- Single `master` branch; feature work is done in short-lived branches with PRs.
- Commit messages use Conventional Commits style: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.
- All packages share the same minor version (`3.1.x`) for compatibility — bump together.

## Domain Context

- This is a **library** — consumers depend on its public surface. Any removal or signature change to a public type/member is a **breaking change**.
- The service pattern enforces single-responsibility: one `CoreServiceBase` subclass per logical operation.
- Long-running orchestration relies on database-persisted state; the MySQL manager polls/resumes suspended services.
- Envelope encryption: a data key is generated per payload, encrypted by KMS, and stored alongside the ciphertext.

## Important Constraints

- **No circular dependencies** — Core must never reference AmazonCloud or MySql.
- **XML doc comments required** on all public API surface — CS1591 treated as a build error.
- **Nullable enabled** — no suppression without explicit justification.
- **Version parity** — all three packages must stay on the same `3.1.x` minor version line.
- MIT license — keep dependencies compatible.

## External Dependencies

| Service | Package | Purpose |
|---------|---------|---------|
| AWS SNS | AWSSDK.SimpleNotificationService | Status event sink |
| AWS SQS | AWSSDK.SQS / Lambda.SQSEvents | Message queue + Lambda trigger |
| AWS S3 | AWSSDK.S3 | Long-running service state store |
| AWS KMS | AWSSDK.KeyManagementService | Envelope encryption key management |
| AWS Secrets Manager | AWSSDK.SecretsManager | Secrets vault implementation |
| AWS SES v2 | AWSSDK.SimpleEmailV2 | Email delivery |
| MySQL | MySql.Data 8.x | Long-running service manager |

<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build BaseLib.sln
dotnet build BaseLib.sln -c Release

# Test
dotnet test
dotnet test --filter "FullyQualifiedName~SomeTestClass"   # single test class

# Pack NuGet packages (output to ../nugetpackages)
dotnet pack BaseLib.sln -o ../nugetpackages -c Debug
dotnet pack BaseLib.sln -o ../nugetpackages -c Release
```

VS Code tasks (`build`, `clean`, `pack`) mirror the above commands.

## Architecture

**BaseLib** is a C# (.NET 8) foundation library distributed as NuGet packages. It is organized into three projects with a strict dependency hierarchy:

```
BaseLib.Core          ← zero cloud/DB deps (platform-agnostic)
├── BaseLib.Core.AmazonCloud   ← AWS implementations (SNS, SQS, S3, KMS, SES, Secrets)
├── BaseLib.Core.MySql         ← MySQL implementations (journal log, long-running manager)
└── BaseLib.Core.Tests         ← xUnit + Moq tests
```

All packages share the same minor version (`3.1.x`) for compatibility.

### Core Service Pattern

Services follow a strict request/response contract:

```csharp
public class MyService : CoreServiceBase<MyRequest, MyResponse>
{
    protected override async Task<MyResponse> RunAsync()
    {
        // use Fail(ReasonCode.X) or Succeed()
        return new MyResponse { Succeeded = true };
    }
}
```

- `TRequest` extends `CoreRequestBase`, `TResponse` extends `CoreResponseBase`
- The framework handles validation, event emission, and error wrapping automatically
- `return Fail(reasonCode)` / `return Succeed()` are the only exit paths needed

### Reason Codes

Use enums with `[Description]` attributes; they implicitly convert to `CoreReasonCode`:

```csharp
public enum MyReasonCode { [Description("Not found")] NotFound = 1 }
return Fail(MyReasonCode.NotFound, "extra context");
```

### Long-Running Services

For multi-step orchestration across async child services:

- Inherit `CoreLongRunningServiceBase<TRequest, TResponse>`
- Fire children: `await FireAsync<TChildService>(childRequest)` or `FireManyAsync(...)`
- The parent **suspends** after firing; state is persisted via `ICoreServiceStateStore` (S3 in AWS)
- Implement `ResumeAsync()` — called by `ICoreLongRunningServiceManager` (MySQL-based) when all children complete

### Interfaces (defined in Core, implemented in extension packages)

| Interface | AWS impl | MySQL impl |
|---|---|---|
| `ICoreStatusEventSink` | `SnsCoreStatusEventSink` (SNS) | — |
| `ICoreServiceStateStore` | `S3CoreServiceStateStore` | — |
| `ICoreLongRunningServiceManager` | — | `MySqlCoreLongRunningServiceManager` |

### Serialization & Security

- `CoreSerializer` — polymorphic JSON (stores `___type` discriminator for open hierarchies)
- `CoreSecureJsonSerializer` — encrypts fields marked `[CoreSecret]` using envelope encryption
- `ICoreSecretsVault` — key management abstraction (AWS Secrets Manager implementation included)

### SQS / Lambda Entry Point

`CoreServiceMessageProcessorBase` (in AmazonCloud) is the standard Lambda handler for SQS-triggered services.

## Documentation Standards

All **public** types and members require XML doc comments (`<summary>`, `<param>` for non-obvious params). The projects build with `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and treat CS1591 as an error — missing comments will break the build.

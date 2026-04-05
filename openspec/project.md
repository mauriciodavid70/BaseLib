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

### Claude Code Workflow

**Triage first:**
- Bug fix / typo / test for existing behavior → implement directly, no proposal needed.
- New capability, breaking change, or architecture shift → follow the three OpenSpec stages below.

**Stage 1 — Proposal (background agent, worktree-isolated):**
1. Main session runs the context checklist: `openspec list`, `openspec list --specs`, review `project.md`.
2. Main session launches a background agent with `isolation: "worktree"`.
3. Agent scaffolds the change and validates: `openspec validate <id> --strict`.
4. **Validation must pass before returning for review** — do not surface the proposal if `--strict` reports errors.
5. Agent returns the proposal to the main session for explicit approval.
6. **After approval**, agent commits only `openspec/changes/<id>/` to the worktree branch — no other files. Untracked files bleed across worktrees, so the proposal must be committed before the implementation agent starts.

**Stage 2 — Implementation (background agent, same worktree branch):**
1. Main session launches a background agent targeting the same worktree branch.
2. Agent reads `proposal.md` → `design.md` (if present) → `tasks.md`, then implements sequentially.
3. Agent checks off every item in `tasks.md` after completion.
4. Agent bumps the version in `Directory.Build.props` (single source of truth for all packages):
   - Breaking change → minor bump, reset patch: `3.1.x → 3.2.0`
   - Feature or fix → patch bump: `3.1.x → 3.1.x+1`
5. Agent finalises: `openspec archive <id> --yes` → `git rebase master` (if master has advanced) → `git push` → `gh pr create`.
6. Agent returns the PR URL to the main session.

**Stage 3 — Review:**
- Main session receives the PR URL; human reviews and merges via GitHub.
- Never merge locally.

**Stage 4 — Release (after PR is merged):**
1. `git pull` master to get merged changes.
2. `dotnet build BaseLib.sln -c Release` — verify clean build before packing.
3. `dotnet pack BaseLib.sln -o ../nugetpackages -c Release`
4. Extract version and push only that version's packages:
   ```bash
   VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props)
   dotnet nuget push "../nugetpackages/BaseLib.*.$VERSION.nupkg" --source nuget.org \
     --api-key $(security find-generic-password -a nuget -s NUGET_PERSONAL_APIKEY -w) \
     --skip-duplicate
   ```

**Hard constraints:**
- No work is ever committed directly to master — all work happens in worktree branches.
- Proposal must be committed inside the worktree before launching the implementation agent (worktree isolation only applies to tracked files).
- Archive + PR steps happen inside the implementation agent, not the main session.
- Always rebase onto master before pushing if master has advanced since the worktree was created.
- `Directory.Build.props` is the single version source — never set `<Version>` in individual `.csproj` files.

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

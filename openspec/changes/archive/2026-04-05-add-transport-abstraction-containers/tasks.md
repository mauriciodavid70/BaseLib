## 1. BaseLib.Core — Transport Abstraction Interfaces and Dispatcher
- [ ] 1.1 Add `IMessageEnvelope` interface (`Body`, `MessageId`) to `BaseLib.Core/Services/`
- [ ] 1.2 Add `CoreMessageDispatcher` class to `BaseLib.Core/Services/` — extract dispatch logic from `CoreServiceMessageProcessorBase.HandleSingleMessageAsync`
- [ ] 1.3 Add abstract `CoreBackgroundService : BackgroundService` to `BaseLib.Core/Services/` with template `ReceiveAsync`, `AcknowledgeAsync`, `NackAsync`
- [ ] 1.4 Add XML doc comments to all new public types and members (CS1591 enforcement)

## 2. BaseLib.Core.AmazonCloud — Refactor Lambda Adapter
- [ ] 2.1 Refactor `CoreServiceMessageProcessorBase` to convert `SQSMessage` → `IMessageEnvelope` and delegate to `CoreMessageDispatcher`
- [ ] 2.2 Verify `HandleAsync(SQSEvent, ILambdaContext)` public signature is unchanged
- [ ] 2.3 Update unit tests in `BaseLib.Core.Tests` to cover `CoreMessageDispatcher` directly

## 3. New Project: BaseLib.Core.Containers
- [ ] 3.1 Create `BaseLib.Core.Containers/BaseLib.Core.Containers.csproj` — references `BaseLib.Core`; no cloud deps; added to `BaseLib.sln`
- [ ] 3.2 Implement `FileSystemCoreServiceStateStore : ICoreServiceStateStore` — JSON serialization, operationId as filename, configurable root directory
- [ ] 3.3 Implement `EnvironmentSecretsVault : ICoreSecretsVault` — reads `Environment.GetEnvironmentVariable(secretName)`
- [ ] 3.4 Implement `SmtpEmailSender : IEmailSender` — MailKit `SmtpClient`, configurable host/port/credentials
- [ ] 3.5 Add `AddContainerServices(this IServiceCollection, ...)` DI extension registering all three implementations
- [ ] 3.6 Add XML doc comments to all public types and members

## 4. Tests
- [ ] 4.1 Add unit tests for `CoreMessageDispatcher` (RunAsync path, ResumeAsync path, unknown method throws)
- [ ] 4.2 Add unit tests for `FileSystemCoreServiceStateStore` (write → read round-trip, missing file throws)
- [ ] 4.3 Add unit tests for `EnvironmentSecretsVault` (variable present, variable absent throws)

## 5. Version and Validation
- [ ] 5.1 Bump version in `Directory.Build.props`: `3.1.x → 3.2.0`
- [ ] 5.2 Run `dotnet build BaseLib.sln` — must be clean (zero errors, zero CS1591 warnings)
- [ ] 5.3 Run `dotnet test` — all tests pass

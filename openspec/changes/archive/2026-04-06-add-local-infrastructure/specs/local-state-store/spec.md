## ADDED Requirements

### Requirement: File System State Store in BaseLib.Core.Local
`FileSystemCoreServiceStateStore` SHALL be moved from `BaseLib.Core.Containers` to
`BaseLib.Core.Local` and SHALL implement `ICoreServiceStateStore` by serializing long-running
service state as JSON files on the local file system, using the `operationId` as the filename.
The default root directory SHALL be `Path.GetTempPath()`.

#### Scenario: Write and read round-trip
- **WHEN** `WriteAsync(operationId, state)` is called followed by `ReadAsync(operationId)`
- **THEN** `ReadAsync` returns the same key-value pairs that were written

#### Scenario: Read missing state throws
- **WHEN** `ReadAsync` is called with an `operationId` for which no file exists
- **THEN** an `InvalidOperationException` is thrown indicating the state was not found

#### Scenario: Default root is temp directory
- **WHEN** `AddLocalServices` is called without configuring `StateStoreRootDirectory`
- **THEN** state files are written under `Path.GetTempPath()`

### Requirement: Local DI Registration
`AddLocalServices` SHALL be an `IServiceCollection` extension method that registers
`FileSystemCoreServiceStateStore` as `ICoreServiceStateStore`. It SHALL accept an optional
`Action<LocalServicesOptions>` to override the state store root directory.

#### Scenario: Default registration uses temp directory
- **WHEN** `AddLocalServices(services)` is called with no configuration action
- **THEN** `ICoreServiceStateStore` resolves to `FileSystemCoreServiceStateStore` writing to `Path.GetTempPath()`

#### Scenario: Custom root directory override
- **WHEN** `AddLocalServices(services, o => o.StateStoreRootDirectory = "/custom")` is called
- **THEN** `ICoreServiceStateStore` resolves to `FileSystemCoreServiceStateStore` writing to `/custom`

## MODIFIED Requirements

### Requirement: Container DI Registration (containers-runtime spec)
`AddContainerServices` SHALL register `EnvironmentSecretsVault` and `SmtpEmailSender` only.
`ICoreServiceStateStore` is no longer registered by `AddContainerServices`; callers that need
a local state store must call `AddLocalServices` separately.
`StateStoreRootDirectory` is removed from `ContainerServicesOptions`.

#### Scenario: State store NOT registered by AddContainerServices
- **WHEN** `AddContainerServices(services, options)` is called during application startup
- **THEN** `ICoreSecretsVault` and `IEmailSender` resolve to their container-runtime implementations
- **AND** `ICoreServiceStateStore` is NOT registered by this call

## ADDED Requirements

### Requirement: File System State Store
`FileSystemCoreServiceStateStore` SHALL implement `ICoreServiceStateStore` by serializing long-running service state as a JSON file under a configurable root directory, using the `operationId` as the filename.

#### Scenario: Write and read round-trip
- **WHEN** `WriteAsync(operationId, state)` is called followed by `ReadAsync(operationId)`
- **THEN** `ReadAsync` returns the same key-value pairs that were written

#### Scenario: Read missing state throws
- **WHEN** `ReadAsync` is called with an `operationId` for which no file exists
- **THEN** an exception is thrown indicating the state was not found

### Requirement: Environment Secrets Vault
`EnvironmentSecretsVault` SHALL implement `ICoreSecretsVault` by reading the environment variable whose name matches the `secretName` parameter.

#### Scenario: Variable present
- **WHEN** `GetSecretValueAsync(secretName)` is called and an environment variable named `secretName` exists
- **THEN** the variable's value is returned

#### Scenario: Variable absent throws
- **WHEN** `GetSecretValueAsync(secretName)` is called and no environment variable named `secretName` exists
- **THEN** an `InvalidOperationException` is thrown

### Requirement: SMTP Email Sender
`SmtpEmailSender` SHALL implement `IEmailSender` by delivering a `MimeMessage` via a configurable SMTP host, port, and optional credentials using MailKit's `SmtpClient`.

#### Scenario: Successful delivery
- **WHEN** `SendAsync(message)` is called with a valid `MimeMessage` and the SMTP server accepts the message
- **THEN** an `EmailResponse` with `Succeeded = true` is returned

#### Scenario: SMTP failure returns failed response
- **WHEN** the SMTP server rejects the message or is unreachable
- **THEN** an `EmailResponse` with `Succeeded = false` is returned

### Requirement: Container DI Registration
`AddContainerServices` SHALL be an `IServiceCollection` extension method that registers `FileSystemCoreServiceStateStore`, `EnvironmentSecretsVault`, and `SmtpEmailSender` as the active implementations of their respective Core interfaces in a single call.

#### Scenario: All implementations registered
- **WHEN** `AddContainerServices(services, options)` is called during application startup
- **THEN** `ICoreServiceStateStore`, `ICoreSecretsVault`, and `IEmailSender` resolve to their container-runtime implementations

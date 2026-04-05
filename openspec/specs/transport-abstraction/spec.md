# transport-abstraction Specification

## Purpose
TBD - created by archiving change add-transport-abstraction-containers. Update Purpose after archive.
## Requirements
### Requirement: Message Envelope Abstraction
`IMessageEnvelope` SHALL provide a `Body` (string) and `MessageId` (string) property as a transport-agnostic wrapper around a raw broker message.

#### Scenario: Envelope exposes body and ID
- **WHEN** a transport adapter wraps a broker message in an `IMessageEnvelope`
- **THEN** `Body` returns the raw JSON payload string and `MessageId` returns a unique identifier for the message

### Requirement: Transport-Agnostic Dispatch
`CoreMessageDispatcher` SHALL accept an `IMessageEnvelope`, deserialize its `Body` via `CoreSerializer`, and route to `ICoreServiceRunner.RunAsync` or `ICoreServiceRunner.ResumeAsync` based on the `Method` field in the payload.

#### Scenario: RunAsync routing
- **WHEN** the envelope body contains `"Method": "RunAsync"` (or the field is absent)
- **THEN** `CoreMessageDispatcher` calls `ICoreServiceRunner.RunAsync` with the deserialized type name, request, correlation ID, and long-running-child flag

#### Scenario: ResumeAsync routing
- **WHEN** the envelope body contains `"Method": "ResumeAsync"`
- **THEN** `CoreMessageDispatcher` calls `ICoreServiceRunner.ResumeAsync` with the deserialized type name and operation ID

#### Scenario: Unknown method throws
- **WHEN** the envelope body contains an unrecognised `Method` value
- **THEN** `CoreMessageDispatcher` throws `NotSupportedException`

### Requirement: Lambda Adapter Preservation
`CoreServiceMessageProcessorBase` SHALL continue to accept an `SQSEvent` batch and return an `SQSBatchResponse` with failed item IDs. It SHALL convert each `SQSEvent.SQSMessage` to an `IMessageEnvelope` and delegate dispatch to `CoreMessageDispatcher`. Its public API SHALL remain unchanged.

#### Scenario: Successful batch processing
- **WHEN** all SQS messages dispatch without error
- **THEN** `HandleAsync` returns an `SQSBatchResponse` with an empty `BatchItemFailures` list

#### Scenario: Partial batch failure
- **WHEN** one message fails dispatch and others succeed
- **THEN** only the failing message's ID appears in `BatchItemFailures`

### Requirement: Hosted Consumer Base
`CoreBackgroundService` SHALL extend `Microsoft.Extensions.Hosting.BackgroundService` and define abstract template methods `ReceiveAsync`, `AcknowledgeAsync`, and `NackAsync` that subclasses implement for their transport. The base class SHALL call `CoreMessageDispatcher.DispatchAsync` after receiving each envelope and call `AcknowledgeAsync` on success or `NackAsync` on failure.

#### Scenario: Successful message processing
- **WHEN** `ReceiveAsync` returns an envelope and dispatch succeeds
- **THEN** `AcknowledgeAsync` is called and the consumer loops to receive the next message

#### Scenario: Dispatch failure
- **WHEN** `CoreMessageDispatcher` throws during dispatch
- **THEN** `NackAsync` is called and the consumer continues without crashing

